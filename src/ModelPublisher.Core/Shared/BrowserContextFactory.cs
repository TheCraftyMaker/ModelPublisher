using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace ModelPublisher.Core.Shared;

public static class BrowserContextFactory
{
    /// <summary>
    /// Launches a persistent Chromium context for the given platform.
    /// The profile is stored under <c>profiles/{platformKey}</c> relative to the working directory,
    /// so sessions survive between runs.
    /// </summary>
    public static async Task<IBrowserContext> GetPersistentContextAsync(
        IPlaywright playwright,
        string platformKey,
        bool headless = false,
        string? profilePathOverride = null)
    {
        var profilePath = Path.GetFullPath(Path.Combine("profiles", platformKey));
        Directory.CreateDirectory(profilePath);

        if (profilePathOverride != null)
        {
            // Copy session cookies + encryption key from the real Brave profile into the platform profile.
            // This lets us bypass Cloudflare using cf_clearance cookies from normal browsing.
            // DPAPI decryption works because we're the same Windows user.
            var profileName = Path.GetFileName(profilePathOverride);
            var userDataDir = Path.GetDirectoryName(profilePathOverride)!;
            var isNamedProfile = profileName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) || profileName == "Default";
            var sourceProfileDir = isNamedProfile ? profilePathOverride : profilePathOverride;
            var sourceUserDataDir = isNamedProfile ? userDataDir : profilePathOverride;

            // Local State holds the AES key used to decrypt cookies (itself DPAPI-protected)
            var localStateSrc = Path.Combine(sourceUserDataDir, "Local State");
            if (File.Exists(localStateSrc))
            {
                File.Copy(localStateSrc, Path.Combine(profilePath, "Local State"), overwrite: true);
                Console.WriteLine($"[BrowserContextFactory] Copied Local State from {localStateSrc}");
            }
            else Console.WriteLine($"[BrowserContextFactory] Local State not found at {localStateSrc}");

            // Cookies
            var cookiesSrc = Path.Combine(sourceProfileDir, "Network", "Cookies");
            if (File.Exists(cookiesSrc))
            {
                var cookiesDest = Path.Combine(profilePath, "Default", "Network");
                Directory.CreateDirectory(cookiesDest);
                File.Copy(cookiesSrc, Path.Combine(cookiesDest, "Cookies"), overwrite: true);
                Console.WriteLine($"[BrowserContextFactory] Copied Cookies from {cookiesSrc}");
            }
            else Console.WriteLine($"[BrowserContextFactory] Cookies not found at {cookiesSrc}");
        }

        var context = await playwright.Chromium.LaunchPersistentContextAsync(profilePath, new()
        {
            Headless = headless,
            SlowMo = 80,
            ExecutablePath = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            Permissions = ["geolocation"],
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        // Remove automation markers and spoof fingerprints to evade Cloudflare bot detection
        await context.AddInitScriptAsync("""
            // 1. Remove webdriver flag
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            delete navigator.__proto__.webdriver;

            // 2. Restore chrome object
            window.chrome = { runtime: {} };

            // 3. Realistic languages
            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });

            // 4. Realistic plugins
            Object.defineProperty(navigator, 'plugins', { get: () => [
                { name: 'Chrome PDF Plugin', filename: 'internal-pdf-viewer', description: 'Portable Document Format' },
                { name: 'Chrome PDF Viewer', filename: 'mhjfbmdgcfjbbpaeojofohoefgiehjai', description: '' }
            ]});

            // 5. Canvas fingerprint noise
            const origToDataURL = HTMLCanvasElement.prototype.toDataURL;
            HTMLCanvasElement.prototype.toDataURL = function(type) {
                const ctx = this.getContext('2d');
                if (ctx) {
                    try {
                        const imageData = ctx.getImageData(0, 0, this.width, this.height);
                        for (let i = 0; i < imageData.data.length; i += 4) {
                            imageData.data[i] += Math.floor(Math.random() * 3) - 1;
                        }
                        ctx.putImageData(imageData, 0, 0);
                    } catch {}
                }
                return origToDataURL.call(this, type);
            };

            // 6. WebGL fingerprint spoofing
            const getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                if (parameter === 37445) return 'Intel Inc.';
                if (parameter === 37446) return 'Intel Iris OpenGL Engine';
                return getParameter.call(this, parameter);
            };
            """);

        return context;
    }

    /// <summary>
    /// Launches Brave via the stealth-launcher.js Node.js script (playwright-extra + stealth plugin)
    /// and connects to it over CDP. Use this for Cloudflare-protected platforms.
    /// </summary>
    public static async Task<IBrowserContext> GetStealthContextAsync(
        IPlaywright playwright,
        string platformKey,
        string? profilePathOverride = null)
    {
        var port = GetFreePort();
        // stealth-launcher.js lives in the project root; during dotnet run the CWD is the project root
        var launcherPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "stealth-launcher.js"));
        var nodeExe = @"C:\Program Files\nodejs\node.exe";

        // Use the real Brave profile if provided, otherwise fall back to the persistent platform profile
        var effectiveProfilePath = profilePathOverride
            ?? Path.GetFullPath(Path.Combine("profiles", platformKey));
        Directory.CreateDirectory(Path.GetFullPath(Path.Combine("profiles", platformKey)));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodeExe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };
        // Use ArgumentList so .NET handles quoting — avoids newline/space issues with paths
        process.StartInfo.ArgumentList.Add(launcherPath);
        process.StartInfo.ArgumentList.Add(port.ToString());
        process.StartInfo.ArgumentList.Add(effectiveProfilePath);
        process.Start();

        // Wait for the READY signal from the launcher
        bool ready = false;
        while (!ready)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (line == null) break;
            if (line.StartsWith("READY:"))
                ready = true;
        }

        if (!ready)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"stealth-launcher did not emit a READY signal. stderr: {stderr}");
        }

        var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://localhost:{port}");
        return browser.Contexts.FirstOrDefault()
            ?? await browser.NewContextAsync();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

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
        bool headless = false)
    {
        var profilePath = Path.GetFullPath(Path.Combine("profiles", platformKey));
        Directory.CreateDirectory(profilePath);

        return await playwright.Chromium.LaunchPersistentContextAsync(profilePath, new()
        {
            Headless = headless,
            SlowMo = 80,
            ExecutablePath = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
    }
}

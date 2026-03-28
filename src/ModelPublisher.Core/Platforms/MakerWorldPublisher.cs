using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for MakerWorld.com (Bambu Labs platform, free tier).
///
/// SETUP NOTES:
/// MakerWorld is a modern React SPA. Upload flows use dynamic components.
/// Run `playwright codegen https://makerworld.com/en/create` to capture live selectors.
/// Pay attention to file upload dropzones — they may not use standard input[type=file].
/// </summary>
public class MakerWorldPublisher : IPlatformPublisher
{
    public string PlatformKey => "makerworld";
    public string PlatformName => "MakerWorld";

    public bool IsFreeOnly => true;
    public bool SupportsMarkdown => true;

    public string Disclaimer => GetDisclaimer();

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page,
        CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://makerworld.com/en/my/models/publish?type=original");

            await AuthGuard
                .EnsureLoggedInAsync(page, PlatformName,
                    async p => await p
                        .Locator("img[src='https://public-cdn.bblmw.com/avatar/d59a9d40-0c79-11ee-9a50-b1cd743d2b1a." +
                                 "jpg?x-oss-process=image/resize,w_60/format,webp']").First
                        .IsVisibleAsync(), ct);

            // Step 1: Specific platform print profile(s)
            await UploadFiles(manifest, page);
            
            

            return new PublishResult(PlatformName, true, page.Url, null);
        }
        catch (Exception ex)
        {
            return new PublishResult(PlatformName, false, null, ex.Message);
        }
    }

    public Task<PublishResult> PublishPremiumAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private async Task UploadFiles(ReleaseManifest manifest, IPage page)
    {
        // Step 1: Specific platform print profile(s)
        var config = manifest.GetPlatformConfig<PlatformConfig>(PlatformKey);
        if (config != null && config.PrintProfiles.Any())
        {
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading MakerWorld specific profile...");

            await page
                .GetByRole(AriaRole.Radio, new() { Name = "Yes (earn extra points reward)" })
                .CheckAsync();

            var profileInput = page.Locator("input[type='file'][accept='.3mf']").First;

            var profilePath = config.PrintProfiles.Select(manifest.ResolveFilePath).First();

            await FileUploadHelper.UploadSequentialAsync(page, profileInput, [profilePath], PlatformName);
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"[cyan][[{PlatformName}]][/] No MakerWorld specific profile found. Skipping...");

            await page
                .GetByRole(AriaRole.Radio, new() { Name = "I have STL/CAD files or other types of 3MF files" })
                .CheckAsync();
        }

        // Step 2: Model files
        AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model files...");

        var pathsToExclude = new List<string>();
        if (config != null && config.PrintProfiles.Any())
        {
            pathsToExclude.AddRange(config.PrintProfiles.Select(manifest.ResolveFilePath));
        }
        
        var nonProfileModelFiles = manifest.Files.Models
            .Select(manifest.ResolveFilePath)
            .Except(pathsToExclude);

        var modelInput = page.Locator(
            "input[type='file'][accept*='.3ds, .amf, .blend, .dwg, .dxf, .f3d, .f3z, .factory, " +
            ".fcstd, .iges, .ipt, .obj, .ply, .py, .rsdoc, .scad, .shape, .shapr, .skp, .sldasm, " +
            ".sldprt, .slvs, .step, .stl, .stp, .studio3, .zip, .3mf, .stpz, .fcstd']").First;

        await FileUploadHelper.UploadSequentialAsync(page, modelInput, nonProfileModelFiles, PlatformName);
        
        await page
            .GetByRole(AriaRole.Button, new() { Name = "Next Step" })
            .ClickAsync();
    }
    
    private static string GetDisclaimer()
    {
        return "_ALL The Crafty Maker designs are protected by Copyright Law. By downloading, YOU HAVE NO RIGHT " +
               "to sell any digital files or reproductions from those files. If you want a commercial license to " +
               "LEGALLY SELL 3D prints, you'll need a [The Crafty Seller Patreon](https://patreon.com/TheCraftyMaker) " +
               "subscription._";
    }
}
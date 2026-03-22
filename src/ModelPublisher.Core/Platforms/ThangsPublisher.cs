using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for Thangs.com (free + premium tiers).
///
/// SETUP NOTES:
/// Thangs supports both free and monetized models. The manifest platform config
/// should include a "tier" field: "free" or "premium".
/// Run `playwright codegen https://thangs.com/designer/upload` to capture selectors.
/// </summary>
public class ThangsPublisher : IPlatformPublisher
{
    public string PlatformKey => "thangs";
    public string PlatformName => "Thangs";

    public bool IsFreeOnly => false;
    public bool SupportsMarkdown => true;

    public string Disclaimer => GetDisclaimer();

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page,
        CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://thangs.com/mythangs");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName,
                p => Task.FromResult(p.Url.Contains("mythangs")), ct);

            await page
                .GetByRole(AriaRole.Button, new() { Name = "Add new" })
                .ClickAsync();

            await page
                .GetByTestId("action-upload-models")
                .ClickAsync();

            // Step 1: Model files
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model file...");

            var modelFileInput = page.Locator("input[multiple]").First;
            await modelFileInput.FocusAsync();

            await FileUploadHelper.UploadSequentialAsync(
                page, modelFileInput, manifest.Files.Models.Select(manifest.ResolveFilePath), PlatformName);

            await page.GetByTestId("upload-mode-collection").ClickAsync();

            await page.Locator("label[for='terms-acceptance']").ClickAsync();

            await page
                .GetByTestId("file-selector-buttons-upload-files")
                .ClickAsync();

            // Step 2: Model Title
            await page
                .GetByTestId("model-upload-name-input")
                .ClearAsync();

            await page
                .GetByTestId("model-upload-name-input")
                .FillAsync(manifest.Title);

            // Step 3: Model Description
            await page
                .GetByTestId("model-upload-description-input")
                .FillAsync(manifest.GetDescription(this));

            // Step 4: Model Category
            // TODO: Manually for now

            // Step 5: Tags
            foreach (var tag in manifest.Tags)
            {
                await page
                    .GetByTestId("cy_tag_input")
                    .FillAsync(tag);

                await page
                    .GetByTestId("cy_tag_input")
                    .PressAsync("Enter");

                await page.WaitForTimeoutAsync(300);
            }

            // Step 6: Upload photos
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");

            var photoInput = page.Locator("input[multiple][accept*='.jpg']").First;
            await FileUploadHelper.UploadSequentialAsync(
                page, photoInput, manifest.Files.PhotosOrdered(coverFirst: false).Select(manifest.ResolveFilePath),
                PlatformName);

            // Step 7: Audience
            await page.Locator("[class*='Audience_VisibilityItem']").First.ClickAsync();
            await page.GetByText("Public sharing").ClickAsync();

            // Step 7: License
            await page.GetByRole(AriaRole.Button, new() { Name = "Select a license" }).ClickAsync();
            await page.GetByText("by-nc-sa_4_0.txt").First.ClickAsync();

            // Step 8: Human review before publishing
            AnsiConsole.MarkupLine(
                $"[yellow][[{PlatformName}]][/] Review the form in the browser. Press [green]Enter[/] to publish...");

            await Task.Run(Console.ReadLine, ct);

            // Step 9: Save
            await page.GetByTestId("save-model-details").ClickAsync();

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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

    private static string GetDisclaimer()
    {
        return "_ALL The Crafty Maker designs are protected by Copyright Law. By downloading, YOU HAVE NO RIGHT to " +
               "sell any digital files or reproductions from those files. If you want a commercial license to LEGALLY " +
               "SELL 3D prints, you'll need a The Crafty Companion subscription._";
    }
}
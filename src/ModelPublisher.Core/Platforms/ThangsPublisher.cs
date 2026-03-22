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

            // Step 1: Model files — intercept file chooser so React registers the selection
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model files...");

            var modelChooser = await page
                .RunAndWaitForFileChooserAsync(() => page.GetByTestId("full-page-upload-container").ClickAsync());
            
            await modelChooser
                .SetFilesAsync(manifest.Files.Models.Select(manifest.ResolveFilePath).ToArray());

            var uploadMode = page.GetByTestId("upload-mode-collection");
            await uploadMode.WaitForAsync();
            await uploadMode.ClickAsync();

            var terms = page.Locator("label[for='terms-acceptance']");
            await terms.WaitForAsync();
            await terms.ClickAsync();
            
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

            // Step 6: Upload photos — set all at once
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");

            var photoInput = page.Locator("input[multiple][accept*='.jpg']").First;
            await FileUploadHelper
                .UploadToInputAsync(photoInput, manifest.Files.PhotosOrdered(coverFirst: false).Select(manifest.ResolveFilePath).ToArray());
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Step 7: Audience — retry clicking the trigger until the dialog opens
            await page
                .GetByTestId("model-upload-audience")
                .WaitForAsync();
            
            var audienceTrigger = page.Locator("span[aria-haspopup='dialog']:has([data-testid='model-upload-audience'])");
            await audienceTrigger.ScrollIntoViewIfNeededAsync();
            var publicSharingOption = page.GetByText("Public sharing");
            for (var attempt = 0; attempt < 5; attempt++)
            {
                await audienceTrigger.ClickAsync();
                try
                {
                    await publicSharingOption.WaitForAsync(new() { Timeout = 2_000 });
                    break;
                }
                catch { /* dropdown didn't open, retry */ }
            }
            await publicSharingOption.ClickAsync();

            // Step 7: License
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Select a license" })
                .ClickAsync();
            
            await page
                .GetByText("by-nc-sa_4_0.txt").First
                .ClickAsync();

            // Step 8: Save — wait for button to be enabled, then click (Thangs uses data-test-id, not data-testid)
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Saving...");
            await Assertions
                .Expect(page.Locator("[data-test-id='save-and-publish-model-details']"))
                .ToBeEnabledAsync(new() { Timeout = 60_000 });
            
            await page
                .Locator("[data-test-id='save-and-publish-model-details']")
                .ClickAsync();
            
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Save clicked.");

            // Step 9: Close the upload dialog (non-fatal if already closed/navigated)
            await page.Locator("[class*='FullPageUpload_Header_CloseIcon']").ClickAsync();
                
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
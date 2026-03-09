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

    public string Disclaimer => "";
    
    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://thangs.com/designer/upload");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName, async p =>
            {
                return await p.Locator("[data-testid='user-menu'], .user-avatar, nav .avatar")
                              .First.IsVisibleAsync();
            }, ct);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling model details...");

            // TODO: Replace selectors after running codegen
            await page.Locator("input[name='name'], input[placeholder*='model name' i]").First.FillAsync(manifest.Title);
            await page.Locator("textarea[name='description'], [placeholder*='description' i]").First.FillAsync(manifest.GetDescription(this));

            // Tags
            foreach (var tag in manifest.Tags)
            {
                var tagInput = page.Locator("input[placeholder*='tag' i], .tags-input input").First;
                await tagInput.FillAsync(tag);
                await tagInput.PressAsync("Enter");
                await page.WaitForTimeoutAsync(300);
            }

            // Model file
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model file...");
            // await page.Locator("input[type='file']").First
            //           .SetInputFilesAsync(manifest.ResolveFilePath(manifest.Files.Model));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Photos
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");
            var photoInput = page.Locator("input[type='file'][accept*='image']").First;
            await FileUploadHelper.UploadSequentialAsync(page, photoInput,
                manifest.Files.Photos.Select(manifest.ResolveFilePath), PlatformName);

            AnsiConsole.MarkupLine($"[yellow][[{PlatformName}]][/] Review the form in the browser. Press [green]Enter[/] to publish...");
            await Task.Run(() => Console.ReadLine(), ct);

            await page.Locator("button:has-text('Publish'), button:has-text('Upload'), button[type='submit']").First.ClickAsync();
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
}

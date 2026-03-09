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

    public string Disclaimer => "";

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://makerworld.com/en/create");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName, async p =>
            {
                return await p.Locator(".user-avatar, [data-testid='user-avatar'], .avatar-wrapper")
                              .First.IsVisibleAsync();
            }, ct);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling model details...");

            // TODO: Replace selectors after running codegen
            await page.Locator("input[name='title'], input[placeholder*='title' i]").First.FillAsync(manifest.Title);

            var descEditor = page.Locator("textarea[name='description'], [contenteditable='true']").First;
            await descEditor.ClickAsync();
            await descEditor.FillAsync(manifest.GetDescription(this));

            // Tags
            foreach (var tag in manifest.Tags)
            {
                var tagInput = page.Locator("input[placeholder*='tag' i]").First;
                await tagInput.FillAsync(tag);
                await tagInput.PressAsync("Enter");
                await page.WaitForTimeoutAsync(300);
            }

            // Model file upload
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model file...");
            var modelInput = page.Locator("input[type='file']").First;
            // await modelInput.SetInputFilesAsync(manifest.ResolveFilePath(manifest.Files.Model));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Photos
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");
            var photoInput = page.Locator("input[type='file'][accept*='image']").First;
            await FileUploadHelper.UploadSequentialAsync(page, photoInput,
                manifest.Files.Photos.Select(manifest.ResolveFilePath), PlatformName);

            AnsiConsole.MarkupLine($"[yellow][[{PlatformName}]][/] Review the form in the browser. Press [green]Enter[/] to publish...");
            await Task.Run(() => Console.ReadLine(), ct);

            await page.Locator("button:has-text('Publish'), button[type='submit']").First.ClickAsync();
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

using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for Maker.online (free tier).
///
/// SETUP NOTES:
/// Smaller platform — likely a simpler form-based UI.
/// Run `playwright codegen https://maker.online` to discover the upload flow and
/// replace the placeholder selectors below.
/// </summary>
public class MakerOnlinePublisher : IPlatformPublisher
{
    public string PlatformKey => "makeronline";
    public string PlatformName => "MakerOnline";
    
    public bool IsFreeOnly => true;
    public bool SupportsMarkdown => true;

    public string Disclaimer => "";

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        try
        {
            // TODO: Replace with actual upload/create URL after inspecting the platform
            await page.GotoAsync("https://maker.online/upload");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName, async p =>
            {
                return await p.Locator(".user-avatar, .logged-in, [data-user]")
                              .First.IsVisibleAsync();
            }, ct);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling model details...");

            // TODO: Replace selectors after running codegen
            await page.Locator("input[name='title'], input[name='name']").First.FillAsync(manifest.Title);
            await page.Locator("textarea[name='description']").First.FillAsync(manifest.GetDescription(this));

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

            await page.Locator("button[type='submit'], button:has-text('Publish')").First.ClickAsync();
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

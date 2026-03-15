using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for Patreon.com (free + premium posts).
///
/// SETUP NOTES:
/// Patreon is the most complex platform in this suite. Their post editor is a
/// rich text (Slate.js) editor, and they have active bot detection.
///
/// Strategy:
/// - Navigate to the post creation page
/// - Fill what we can reliably (title, access tier selection, file attachments)
/// - Paste description into the editor via clipboard (more reliable than FillAsync on rich editors)
/// - Always require human review before publishing — do NOT attempt full automation here
///
/// For the access tier: set "access_tier_id" in the manifest platform config.
/// For free posts visible to all: set "free_post": true
///
/// Run `playwright codegen https://www.patreon.com/posts/create` to capture current selectors.
/// Expect this script to need the most maintenance of all platforms.
/// </summary>
public class PatreonPublisher : IPlatformPublisher
{
    public string PlatformKey => "patreon";
    public string PlatformName => "Patreon";
    
    public bool IsFreeOnly => true; // Patreon posts aren't free, but we do not have both free and premium tiers
    public bool SupportsMarkdown => false;

    public string Disclaimer => "";

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        try
        {
            // TODO: read manifest.GetPlatformConfig<PatreonConfig>(PlatformKey) for FreePost and AccessTierId when Patreon automation is implemented

            await page.GotoAsync("https://www.patreon.com/posts/create");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName, async p =>
            {
                return await p.Locator("[data-tag='user-avatar'], .UserAvatar, [aria-label*='profile' i]")
                              .First.IsVisibleAsync();
            }, ct);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling post details...");

            // Title input
            await page.Locator("input[placeholder*='title' i], [data-tag='post-title-input']").First.FillAsync(manifest.Title);

            // Rich text editor — use clipboard paste for reliability
            var editor = page.Locator("[contenteditable='true'], [data-tag='post-body-editor']").First;
            await editor.ClickAsync();

            // Set clipboard content and paste
            await page.EvaluateAsync("text => navigator.clipboard.writeText(text)", manifest.GetDescription(this));
            await page.Keyboard.PressAsync("Control+v");
            await page.WaitForTimeoutAsync(500);

            // File attachment
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Attaching model file...");
            var fileInput = page.Locator("input[type='file']").First;
            // await fileInput.SetInputFilesAsync(manifest.ResolveFilePath(manifest.Files.Model));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Image attachments
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Attaching photos...");
            var photoInput = page.Locator("input[type='file'][accept*='image']").First;
            await FileUploadHelper.UploadSequentialAsync(page, photoInput,
                manifest.Files.Photos.Select(manifest.ResolveFilePath), PlatformName);

            // NOTE: Access tier selection is highly dynamic in Patreon's UI.
            // Manual selection is expected here — the human review step covers this.
            AnsiConsole.MarkupLine($"[yellow][[{PlatformName}]][/] [bold]Important:[/] Please verify access tier and post settings manually.");
            AnsiConsole.MarkupLine($"[yellow][[{PlatformName}]][/] Review the post in the browser. Press [green]Enter[/] to publish...");
            await Task.Run(() => Console.ReadLine(), ct);

            await page.Locator("button:has-text('Publish'), [data-tag='publish-button']").First.ClickAsync();
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

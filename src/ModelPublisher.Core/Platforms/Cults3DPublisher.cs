using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for Cults3D.com (free tier).
///
/// SETUP NOTES:
/// Cults3D has a more traditional server-rendered stack which tends to be
/// more stable for automation. Run `playwright codegen https://cults3d.com/en/3d-model/new`
/// to capture current selectors.
/// Note: Cults3D has a "free" vs paid model distinction controlled by a toggle on the form.
/// </summary>
public class Cults3DPublisher : IPlatformPublisher
{
    public string PlatformKey => "cults3d";
    public string PlatformName => "Cults3D";
    
    public bool IsFreeOnly => true;
    public bool SupportsMarkdown => true;

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://cults3d.com/en/3d-model/new");

            await AuthGuard.EnsureLoggedInAsync(page, PlatformName, async p =>
            {
                return await p.Locator(".current-user, [data-user], .user-nav")
                              .First.IsVisibleAsync();
            }, ct);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling model details...");

            // TODO: Replace selectors after running codegen
            await page.Locator("input[name='creation[name]'], input#creation_name").First.FillAsync(manifest.Title);
            await page.Locator("textarea[name='creation[description]'], #creation_description").First.FillAsync(manifest.GetDescription(this));

            // License
            // await page.Locator("select[name='creation[license]']").SelectOptionAsync(new[] { manifest.License });

            // Tags
            foreach (var tag in manifest.Tags)
            {
                var tagInput = page.Locator("input[name*='tag'], .tag-input input").First;
                await tagInput.FillAsync(tag);
                await tagInput.PressAsync("Enter");
                await page.WaitForTimeoutAsync(300);
            }

            // Model file
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model file...");
            // await page.Locator("input[type='file'][name*='file'], input#creation_files").First
            //           .SetInputFilesAsync(manifest.ResolveFilePath(manifest.Files.Model));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Photos
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");
            var photoInput = page.Locator("input[type='file'][accept*='image'], input#creation_images").First;
            await FileUploadHelper.UploadSequentialAsync(page, photoInput,
                manifest.Files.Photos.Select(manifest.ResolveFilePath), PlatformName);

            AnsiConsole.MarkupLine($"[yellow][[{PlatformName}]][/] Review the form in the browser. Press [green]Enter[/] to publish...");
            await Task.Run(() => Console.ReadLine(), ct);

            await page.Locator("input[type='submit'], button[type='submit']").First.ClickAsync();
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

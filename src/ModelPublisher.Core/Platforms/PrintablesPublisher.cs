using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core.Platforms;

/// <summary>
/// Publisher for Printables.com (free and premium tiers).
/// </summary>
public class PrintablesPublisher : IPlatformPublisher
{
    public string PlatformKey => "printables";
    public string PlatformName => "Printables";

    public bool IsFreeOnly => false;
    public bool SupportsMarkdown => false;

    public string Disclaimer => GetDisclaimer();

    public async Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page,
        CancellationToken ct = default)
    {
        try
        {
            await page.GotoAsync("https://www.printables.com/model/create");

            await AuthGuard
                .EnsureLoggedInAsync(page, PlatformName, 
                    async p => await p.Locator("[data-cy='user-menu'], .user-avatar, [aria-label='User menu']").First.IsVisibleAsync(), ct);

            // Step 1: Model files
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading model file...");

            var modelFileInput = page.Locator(
                "input[type='file'][accept*='.3mf'], input[type='file'][accept*='.stl'], " +
                "input[type='file'][accept*='.obj'], input[type='file'][accept*='.zip']").First;

            await FileUploadHelper.UploadSequentialAsync(
                page, modelFileInput, manifest.Files.Models.Select(manifest.ResolveFilePath), PlatformName);

            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Filling model details...");

            // Step 2: Model Title
            await page
                .GetByRole(AriaRole.Textbox, new() { Name = "Model name (required)" })
                .FillAsync(manifest.Title);

            // Step 3: Model Summary
            await page
                .GetByRole(AriaRole.Textbox, new() { Name = "Summary (required)" })
                .FillAsync(manifest.Title);

            // Step 4: Model Category
            // TODO: Manually for now
            
            // Step 5: Tags
            await page
                .GetByRole(AriaRole.Textbox, new() { Name = "Additional tags" })
                .ClickAsync();
            
            foreach (var tag in manifest.Tags)
            {
                await page
                    .GetByRole(AriaRole.Textbox, new() { Name = "Additional tags" })
                    .FillAsync(tag);
                
                await page
                    .GetByRole(AriaRole.Textbox, new() { Name = "Additional tags" })
                    .PressAsync("Enter");
                
                await page.WaitForTimeoutAsync(300);
            }

            // Step 6: Original model?
            await page
                .GetByRole(AriaRole.Radio, new() { Name = "Original model – I made it" })
                .CheckAsync();

            // Step 7: AI generated?
            await page
                .GetByRole(AriaRole.Radio, new() { Name = "No — fully human-made" })
                .CheckAsync();

            // Step 8: Description — inject HTML directly into TipTap's ProseMirror div
            var descHtml = MarkdownHelper.ToTipTapHtml(manifest.GetDescriptionMarkdown(this));
            var descEditor = page.Locator("section")
                .Filter(new() { HasText = "Description" })
                .GetByRole(AriaRole.Textbox);

            await descEditor.ClickAsync();
            await descEditor.EvaluateAsync(@"(el, html) => {
                el.innerHTML = html;
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            }", descHtml);


            // Step 9: Upload photos
            AnsiConsole.MarkupLine($"[cyan][[{PlatformName}]][/] Uploading photos...");

            var photoInput = page.Locator("input[type='file'][id='photos-upload-input']").First;
            await FileUploadHelper.UploadSequentialAsync(
                page, photoInput, manifest.Files.PhotosOrdered(coverFirst: false).Select(manifest.ResolveFilePath), PlatformName);

            // Step 10: License
            await page
                .GetByRole(AriaRole.Button, new() { Name = "License (required)" })
                .ClickAsync();
            
            await page.
                GetByRole(AriaRole.Button, new() { Name = "Creative Commons — Attribution — Noncommercial — Share Alike" })
                .ClickAsync();

            // Step 11: Submit
            await page
                .GetByRole(AriaRole.Button, new() { Name = "Save draft" })
                .ClickAsync();
   
            // Wait for URL to change away from /model/create to the saved model URL (e.g. /model/12345-slug)
            await page.WaitForURLAsync(url => url.Contains("/model/") && !url.Contains("/model/create"), new() { Timeout = 30_000 });

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
               "SELL 3D prints, you'll need a The Crafty Seller Patreon subscription._";
    }
}
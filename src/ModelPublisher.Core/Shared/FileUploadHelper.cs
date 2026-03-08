using Microsoft.Playwright;
using Spectre.Console;

namespace ModelPublisher.Core.Shared;

public static class FileUploadHelper
{
    /// <summary>
    /// Uploads files to a standard file input element.
    /// </summary>
    public static async Task UploadToInputAsync(ILocator fileInput, params string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Upload file not found: {path}");
        }

        await fileInput.SetInputFilesAsync(filePaths);
    }

    /// <summary>
    /// Uploads files one at a time to a file input, waiting for network idle between each.
    /// Useful for platforms that process uploads asynchronously.
    /// </summary>
    public static async Task UploadSequentialAsync(
        IPage page,
        ILocator fileInput,
        IEnumerable<string> filePaths,
        string platformName)
    {
        foreach (var path in filePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Upload file not found: {path}");

            AnsiConsole.MarkupLine($"  [dim]Uploading {Path.GetFileName(path)}...[/] to {platformName}");
            
            await fileInput.SetInputFilesAsync(path);
            
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(300);
        }
    }
}

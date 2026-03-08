using Microsoft.Playwright;
using Spectre.Console;

namespace ModelPublisher.Core.Shared;

public static class AuthGuard
{
    /// <summary>
    /// Checks if the user is logged in via <paramref name="checkFn"/>.
    /// If not, prompts for manual login and re-checks before continuing.
    /// </summary>
    public static async Task EnsureLoggedInAsync(
        IPage page,
        string platformName,
        Func<IPage, Task<bool>> checkFn,
        CancellationToken ct = default)
    {
        if (await checkFn(page)) return;

        AnsiConsole.MarkupLine($"[yellow][[{platformName}]][/] Not logged in.");
        AnsiConsole.MarkupLine("Log in manually in the browser window, then press [green]Enter[/] to continue...");

        await Task.Run(() => Console.ReadLine(), ct);

        if (!await checkFn(page))
            throw new InvalidOperationException($"[{platformName}] Login not confirmed after manual intervention. Aborting.");

        AnsiConsole.MarkupLine($"[green][[{platformName}]][/] Login confirmed.");
    }
}

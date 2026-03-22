using System.CommandLine;
using Microsoft.Playwright;
using ModelPublisher.Core;
using ModelPublisher.Core.Shared;
using Spectre.Console;

var manifestArg = new Argument<FileInfo>("manifest")
{
    Description = "Path to the release manifest JSON file."
};

var platformsOption = new Option<string[]>("--platforms")
{
    Description = "Limit publishing to specific platforms (by key or name).",
    AllowMultipleArgumentsPerToken = true
};
platformsOption.Aliases.Add("-p");

var rootCommand = new RootCommand("ModelPublisher — automates 3D model publishing across platforms.")
{
    manifestArg,
    platformsOption
};

rootCommand.SetAction(async (parseResult, ct) =>
{
    var manifest = parseResult.GetValue(manifestArg)!;
    var platforms = parseResult.GetValue(platformsOption) ?? [];

    AnsiConsole.Write(
        new FigletText("ModelPublisher")
            .LeftJustified()
            .Color(Color.Cyan1));

    var command = new PublishCommand();
    return await command.ExecuteAsync(
        manifest.FullName,
        platforms.Length > 0 ? platforms : null,
        ct
    );
});

// codegen <platformKey> <url> — opens Brave with the platform's persistent profile and pauses for inspection
var codegenPlatformArg = new Argument<string>("platform") { Description = "Platform key (e.g. thangs)" };
var codegenUrlArg = new Argument<string>("url") { Description = "URL to navigate to" };
var codegenCommand = new Command("codegen", "Open Brave with a platform profile for selector inspection")
{
    codegenPlatformArg,
    codegenUrlArg
};
codegenCommand.SetAction(async (parseResult, ct) =>
{
    var platformKey = parseResult.GetValue(codegenPlatformArg)!;
    var url = parseResult.GetValue(codegenUrlArg)!;

    AnsiConsole.MarkupLine($"[cyan]Opening Brave with profile '[bold]{platformKey}[/]' at {Markup.Escape(url)}[/]");
    AnsiConsole.MarkupLine("[yellow]Use the Playwright Inspector to inspect selectors. Close the browser when done.[/]");

    using var playwright = await Playwright.CreateAsync();
    var context = await BrowserContextFactory.GetPersistentContextAsync(playwright, platformKey);
    var page = await context.NewPageAsync();
    await page.GotoAsync(url);
    await page.PauseAsync();
    await context.CloseAsync();
    return 0;
});
rootCommand.Add(codegenCommand);

return await rootCommand.Parse(args).InvokeAsync();
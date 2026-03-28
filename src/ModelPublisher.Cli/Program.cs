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

var profilePathOption = new Option<string?>("--profile-path")
{
    Description = "Use a custom browser profile directory (e.g. your real Brave profile) to bypass Cloudflare. Close Brave first."
};

var stealthOption = new Option<bool>("--stealth")
{
    Description = "Launch via playwright-extra stealth plugin to bypass Cloudflare bot detection."
};

var rootCommand = new RootCommand("ModelPublisher — automates 3D model publishing across platforms.")
{
    manifestArg,
    platformsOption,
    profilePathOption,
    stealthOption
};

rootCommand.SetAction(async (parseResult, ct) =>
{
    var manifest = parseResult.GetValue(manifestArg)!;
    var platforms = parseResult.GetValue(platformsOption) ?? [];

    AnsiConsole.Write(
        new FigletText("ModelPublisher")
            .LeftJustified()
            .Color(Color.Cyan1));

    var profilePath = parseResult.GetValue(profilePathOption);
    var stealth = parseResult.GetValue(stealthOption);
    var command = new PublishCommand();
    return await command.ExecuteAsync(
        manifest.FullName,
        platforms.Length > 0 ? platforms : null,
        profilePath,
        stealth,
        ct
    );
});

// codegen <platformKey> <url> — opens Brave with the platform's persistent profile and pauses for inspection
var codegenPlatformArg = new Argument<string>("platform") { Description = "Platform key (e.g. thangs)" };
var codegenUrlArg = new Argument<string>("url") { Description = "URL to navigate to" };
var codegenProfileOption = new Option<string?>("--profile-path")
{
    Description = "Override the profile directory (e.g. point to your real Brave profile while it's closed)."
};
var codegenStealthOption = new Option<bool>("--stealth")
{
    Description = "Use playwright-extra stealth plugin (for Cloudflare-protected sites)."
};

var codegenCommand = new Command("codegen", "Open Brave with a platform profile for selector inspection")
{
    codegenPlatformArg,
    codegenUrlArg,
    codegenProfileOption,
    codegenStealthOption
};
codegenCommand.SetAction(async (parseResult, ct) =>
{
    var platformKey = parseResult.GetValue(codegenPlatformArg)!;
    var url = parseResult.GetValue(codegenUrlArg)!;
    var profilePath = parseResult.GetValue(codegenProfileOption);
    var stealth = parseResult.GetValue(codegenStealthOption);

    AnsiConsole.MarkupLine($"[cyan]Opening Brave with profile '[bold]{platformKey}[/]' at {Markup.Escape(url)}[/]");
    if (stealth) AnsiConsole.MarkupLine("[yellow]Stealth mode enabled (playwright-extra)[/]");
    if (profilePath != null) AnsiConsole.MarkupLine($"[yellow]Using custom profile: {Markup.Escape(profilePath)}[/]");
    AnsiConsole.MarkupLine("[yellow]Use the Playwright Inspector to inspect selectors. Close the browser when done.[/]");

    using var playwright = await Playwright.CreateAsync();
    var context = stealth
        ? await BrowserContextFactory.GetStealthContextAsync(playwright, platformKey, profilePath)
        : await BrowserContextFactory.GetPersistentContextAsync(playwright, platformKey, profilePathOverride: profilePath);
    var page = await context.NewPageAsync();
    await page.GotoAsync(url);
    await page.PauseAsync();
    await context.CloseAsync();
    return 0;
});
rootCommand.Add(codegenCommand);

return await rootCommand.Parse(args).InvokeAsync();
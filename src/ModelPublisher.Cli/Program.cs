using System.CommandLine;
using ModelPublisher.Core;
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

return await rootCommand.Parse(args).InvokeAsync();
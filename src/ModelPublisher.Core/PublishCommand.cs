using System.Text.Json;
using Microsoft.Playwright;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Platforms;
using ModelPublisher.Core.Shared;
using Spectre.Console;

namespace ModelPublisher.Core;

public class PublishCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly List<IPlatformPublisher> _publishers =
    [
        new PrintablesPublisher(),
        new MakerWorldPublisher(),
        new Cults3DPublisher(),
        new PatreonPublisher(),
        new ThangsPublisher(),
        new MakerOnlinePublisher()
    ];

    public async Task<int> ExecuteAsync(
        string manifestPath,
        string[]? onlyPlatforms,
        CancellationToken ct = default)
    {
        if (!File.Exists(manifestPath))
        {
            AnsiConsole.MarkupLine($"[red]Manifest not found:[/] {manifestPath}");
            return 1;
        }

        ReleaseManifest manifest;
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct);
            manifest = JsonSerializer.Deserialize<ReleaseManifest>(json, JsonOptions)
                       ?? throw new InvalidDataException("Manifest deserialized to null.");
            manifest.ManifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load manifest:[/] {ex.Message}");
            return 1;
        }

        // Determine which publishers to run and resolve tier per platform
        var selectedPublishers = _publishers
            .Where(p => manifest.Platforms.ContainsKey(p.PlatformKey))
            .Where(p => onlyPlatforms == null
                        || onlyPlatforms.Contains(p.PlatformKey, StringComparer.OrdinalIgnoreCase)
                        || onlyPlatforms.Contains(p.PlatformName, StringComparer.OrdinalIgnoreCase))
            .Select(p => (Publisher: p, Tier: ResolveTier(manifest, p)))
            .Where(x => x.Tier != null)
            .ToList();

        if (selectedPublishers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No matching platforms found in manifest.[/]");
            return 0;
        }

        // Warn about free-only platforms configured as premium
        foreach (var (publisher, tier) in selectedPublishers)
        {
            if (publisher.IsFreeOnly && tier == "premium")
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {publisher.PlatformName} is free-only — skipping premium workflow.");
        }

        var runnablePublishers = selectedPublishers
            .Where(x => !(x.Publisher.IsFreeOnly && x.Tier == "premium"))
            .ToList();

        AnsiConsole.Write(new Rule($"[bold]Publishing:[/] {manifest.Title}").LeftJustified());
        AnsiConsole.MarkupLine($"Platforms: [cyan]{string.Join(", ", runnablePublishers.Select(x => $"{x.Publisher.PlatformName} ({x.Tier})"))}[/]");
        AnsiConsole.WriteLine();

        var session = new PublishSession { ManifestPath = manifestPath };

        using var playwright = await Playwright.CreateAsync();

        foreach (var (publisher, tier) in runnablePublishers)
        {
            AnsiConsole.Write(new Rule($"[bold]{publisher.PlatformName}[/] [dim]({tier})[/]").LeftJustified());

            await using var context = await BrowserContextFactory.GetPersistentContextAsync(
                playwright, publisher.PlatformKey);

            var page = await context.NewPageAsync();

            var result = tier == "premium"
                ? await publisher.PublishPremiumAsync(manifest, page, ct)
                : await publisher.PublishFreeAsync(manifest, page, ct);

            session.Results.Add(result with { Tier = tier! });
            
            if (result.Success)
                AnsiConsole.MarkupLine($"[green]✓ {result.Platform}[/] → {Markup.Escape(result.PublishedUrl ?? "")}");
            else
                AnsiConsole.MarkupLine($"[red]✗ {result.Platform}[/] — {Markup.Escape(result.ErrorMessage ?? "")}");
           
            AnsiConsole.WriteLine();
        }

        // Summary
        AnsiConsole.Write(new Rule("[bold]Summary[/]").LeftJustified());
        var table = new Table()
            .AddColumn("Platform")
            .AddColumn("Tier")
            .AddColumn("Status")
            .AddColumn("URL");

        foreach (var r in session.Results)
        {
            var status = r.Success ? "[green]Published[/]" : "[red]Failed[/]";
            var url = Markup.Escape(r.PublishedUrl ?? r.ErrorMessage ?? "-");
            table.AddRow(r.Platform, r.Tier, status, url);
        }

        AnsiConsole.Write(table);

        var resultsPath = Path.Combine(
            manifest.ManifestDirectory ?? ".",
            $"publish-results-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

        await File.WriteAllTextAsync(
            resultsPath,
            JsonSerializer.Serialize(session, JsonOptions),
            ct);

        AnsiConsole.MarkupLine($"[dim]Results written to {resultsPath}[/]");

        return session.Results.All(r => r.Success) ? 0 : 1;
    }

    /// <summary>
    /// Reads the "tier" field from the platform config in the manifest.
    /// Returns "free" or "premium". Defaults to "free" if not specified.
    /// Returns null if the tier value is unrecognised.
    /// </summary>
    private static string? ResolveTier(ReleaseManifest manifest, IPlatformPublisher publisher)
    {
        var config = manifest.GetPlatformConfig<PlatformConfig>(publisher.PlatformKey);
        if (config is null) return null;
        var tier = config.Tier.ToLowerInvariant();
        return tier is "free" or "premium" ? tier : "free";
    }
}
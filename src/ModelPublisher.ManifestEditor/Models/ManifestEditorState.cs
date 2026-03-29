using System.Text;
using System.Text.Json;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Platforms;

namespace ModelPublisher.ManifestEditor.Models;

public class ManifestEditorState
{
    public static readonly string[] AllPlatformKeys =
        ["printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon"];

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public string License { get; set; } = "CC-BY-4.0";
    public string ManifestDirectory { get; set; } = "";
    public List<string> ModelFiles { get; set; } = [];
    public List<string> Photos { get; set; } = [];
    public string? Cover { get; set; }
    public List<PlatformState> Platforms { get; set; } = [];

    public static ManifestEditorState FromManifest(ReleaseManifest manifest)
    {
        var state = new ManifestEditorState
        {
            Title = manifest.Title ?? "",
            Description = manifest.Description ?? "",
            Tags = manifest.Tags ?? [],
            License = manifest.License ?? "CC-BY-4.0",
            ManifestDirectory = manifest.ManifestDirectory ?? "",
        };

        state.ModelFiles = manifest.Files.Models.Select(manifest.ResolveFilePath).ToList();
        state.Photos = manifest.Files.Photos.Select(manifest.ResolveFilePath).ToList();
        state.Cover = manifest.Files.Cover is not null
            ? manifest.ResolveFilePath(manifest.Files.Cover)
            : null;

        state.Platforms = AllPlatformKeys.Select(key =>
        {
            var config = manifest.GetPlatformConfig<PlatformConfig>(key);
            var platform = new PlatformState
            {
                PlatformKey = key,
                IsEnabled = config is not null,
                Tier = config?.Tier ?? "free",
            };

            if (key == "patreon")
            {
                var patreonConfig = manifest.GetPlatformConfig<PatreonConfig>(key);
                if (patreonConfig is not null)
                {
                    platform.FreePost = patreonConfig.FreePost;
                    platform.AccessTierId = patreonConfig.AccessTierId;
                }
            }

            return platform;
        }).ToList();

        return state;
    }

    public string ToJson()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        writer.WriteString("title", Title);
        writer.WriteString("description", Description);
        writer.WriteString("license", License);

        writer.WriteStartArray("tags");
        foreach (var tag in Tags)
            writer.WriteStringValue(tag);
        writer.WriteEndArray();

        writer.WriteStartObject("files");

        writer.WriteStartArray("models");
        foreach (var file in ModelFiles)
            writer.WriteStringValue(ToRelativePath(file));
        writer.WriteEndArray();

        writer.WriteStartArray("photos");
        foreach (var photo in Photos)
            writer.WriteStringValue(ToRelativePath(photo));
        writer.WriteEndArray();

        if (Cover is not null)
            writer.WriteString("cover", ToRelativePath(Cover));

        writer.WriteEndObject(); // files

        writer.WriteStartObject("platforms");
        foreach (var platform in Platforms.Where(p => p.IsEnabled))
        {
            writer.WriteStartObject(platform.PlatformKey);
            writer.WriteString("tier", platform.Tier);

            if (platform.PlatformKey == "patreon")
            {
                writer.WriteBoolean("free_post", platform.FreePost ?? true);
                if (platform.AccessTierId is not null)
                    writer.WriteString("access_tier_id", platform.AccessTierId);
            }

            writer.WriteEndObject();
        }
        writer.WriteEndObject(); // platforms

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private string ToRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(ManifestDirectory))
            return absolutePath;

        var relative = Path.GetRelativePath(ManifestDirectory, absolutePath);
        relative = relative.Replace('\\', '/');
        if (!relative.StartsWith("./") && !relative.StartsWith("../"))
            relative = "./" + relative;
        return relative;
    }
}

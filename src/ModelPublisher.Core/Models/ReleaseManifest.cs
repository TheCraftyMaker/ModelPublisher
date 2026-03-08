using System.Text.Json;
using System.Text.Json.Serialization;
using ModelPublisher.Core.Platforms;
using ModelPublisher.Core.Shared;

namespace ModelPublisher.Core.Models;

public class ReleaseManifest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("description")] 
    public string Description { private get; set; } = "";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonPropertyName("license")]
    public string License { get; init; } = "CC-BY-4.0";

    [JsonPropertyName("files")]
    public ManifestFiles Files { get; init; } = new();

    [JsonPropertyName("platforms")]
    public Dictionary<string, JsonElement> Platforms { get; init; } = [];

    /// <summary>
    /// Resolves a relative file path in the manifest against the manifest's own directory.
    /// </summary>
    [JsonIgnore]
    public string? ManifestDirectory { get; set; }

    public string ResolveFilePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) return relativePath;
        if (ManifestDirectory is null) return relativePath;
        return Path.GetFullPath(Path.Combine(ManifestDirectory, relativePath));
    }
    
    public string GetDescription(IPlatformPublisher publisher)
        => publisher.SupportsMarkdown
            ? Description
            : MarkdownHelper.ToPlainText(Description);
}

public class ManifestFiles
{
    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = [];

    [JsonPropertyName("photos")]
    public List<string> Photos { get; set; } = [];
}

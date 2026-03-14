using System.Text.Json.Serialization;

namespace ModelPublisher.Core.Models;

public record PlatformConfig
{
    [JsonPropertyName("tier")]
    public string Tier { get; init; } = "free";

    [JsonPropertyName("print_profiles")]
    public List<string> PrintProfiles { get; init; } = [];
}

using System.Text.Json.Serialization;
using ModelPublisher.Core.Models;

namespace ModelPublisher.Core.Platforms;

public record PatreonConfig : PlatformConfig
{
    [JsonPropertyName("free_post")]
    public bool FreePost { get; init; } = true;

    [JsonPropertyName("access_tier_id")]
    public string? AccessTierId { get; init; }
}

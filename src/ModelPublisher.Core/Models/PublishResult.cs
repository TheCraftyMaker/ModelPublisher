namespace ModelPublisher.Core.Models;

public record PublishResult(
    string Platform,
    bool Success,
    string? PublishedUrl,
    string? ErrorMessage
)
{
    /// <summary>Set by the orchestrator after the publisher returns.</summary>
    public string Tier { get; init; } = "free";
}

public class PublishSession
{
    public string ManifestPath { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public List<PublishResult> Results { get; set; } = [];
}
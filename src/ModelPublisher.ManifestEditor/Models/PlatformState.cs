namespace ModelPublisher.ManifestEditor.Models;

public class PlatformState
{
    public string PlatformKey { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Tier { get; set; } = "free";

    public List<string> PrintProfiles { get; set; } = [];

    // Patreon-specific
    public bool? FreePost { get; set; }
    public string? AccessTierId { get; set; }
}

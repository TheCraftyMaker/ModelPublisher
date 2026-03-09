using Microsoft.Playwright;
using ModelPublisher.Core.Models;

namespace ModelPublisher.Core.Platforms;

public interface IPlatformPublisher
{
    /// <summary>
    /// Unique key matching the key used in manifest.json's "platforms" dictionary.
    /// Use lowercase, e.g. "printables", "makerworld".
    /// </summary>
    string PlatformKey { get; }

    string PlatformName { get; }
    
    bool IsFreeOnly => false;
    
    bool SupportsMarkdown => false;

    /// <summary>
    /// Platform-specific disclaimer appended to the bottom of every description (markdown format).
    /// Return empty string to omit.
    /// </summary>
    string Disclaimer => "";

    Task<PublishResult> PublishFreeAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default);
    
    Task<PublishResult> PublishPremiumAsync(ReleaseManifest manifest, IPage page, CancellationToken ct = default);
}

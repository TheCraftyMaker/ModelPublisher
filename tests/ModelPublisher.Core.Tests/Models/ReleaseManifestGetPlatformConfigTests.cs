using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;
using ModelPublisher.Core.Platforms;
using Xunit;

namespace ModelPublisher.Core.Tests.Models;

public class ReleaseManifestGetPlatformConfigTests
{
    private static ReleaseManifest DeserializeManifest(string platformsJson)
    {
        var json = $$"""
            {
              "title": "Test",
              "platforms": {{platformsJson}}
            }
            """;
        return JsonSerializer.Deserialize<ReleaseManifest>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void GetPlatformConfig_AbsentKey_ReturnsNull()
    {
        var manifest = DeserializeManifest("""{"printables":{"tier":"free"}}""");
        manifest.GetPlatformConfig<PlatformConfig>("makerworld").Should().BeNull();
    }

    [Fact]
    public void GetPlatformConfig_BaseType_DeserializesTierAndProfiles()
    {
        var manifest = DeserializeManifest(
            """{"printables":{"tier":"premium","print_profiles":["./profile.3mf"]}}""");
        var config = manifest.GetPlatformConfig<PlatformConfig>("printables");
        config!.Tier.Should().Be("premium");
        config.PrintProfiles.Should().Equal("./profile.3mf");
    }

    [Fact]
    public void GetPlatformConfig_DerivedType_DeserializesExtraFields()
    {
        var manifest = DeserializeManifest(
            """{"patreon":{"tier":"premium","free_post":false,"access_tier_id":"t123"}}""");
        var config = manifest.GetPlatformConfig<PatreonConfig>("patreon");
        config!.Tier.Should().Be("premium");
        config.FreePost.Should().BeFalse();
        config.AccessTierId.Should().Be("t123");
    }

    [Fact]
    public void GetPlatformConfig_NoOptionalFields_ReturnsDefaults()
    {
        var manifest = DeserializeManifest("""{"printables":{}}""");
        var config = manifest.GetPlatformConfig<PlatformConfig>("printables");
        config!.Tier.Should().Be("free");
        config.PrintProfiles.Should().BeEmpty();
    }
}

using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Platforms;
using Xunit;

namespace ModelPublisher.Core.Tests.Platforms;

public class PatreonConfigTests
{
    [Fact]
    public void Deserialize_WithAllFields_PopulatesCorrectly()
    {
        var json = """{"tier":"premium","free_post":false,"access_tier_id":"tier_abc123"}""";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.Tier.Should().Be("premium");
        config.FreePost.Should().BeFalse();
        config.AccessTierId.Should().Be("tier_abc123");
        config.PrintProfiles.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithNoOptionalFields_UsesDefaults()
    {
        var json = "{}";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.Tier.Should().Be("free");
        config.FreePost.Should().BeTrue();
        config.AccessTierId.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InheritsBasePrintProfiles()
    {
        var json = """{"print_profiles":["./x.3mf"]}""";
        var config = JsonSerializer.Deserialize<PatreonConfig>(json);
        config!.PrintProfiles.Should().Equal("./x.3mf");
    }
}

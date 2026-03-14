using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;
using Xunit;

namespace ModelPublisher.Core.Tests.Models;

public class PlatformConfigTests
{
    [Fact]
    public void Deserialize_WithTierAndProfiles_PopulatesBothFields()
    {
        var json = """{"tier":"premium","print_profiles":["./a.3mf","./b.3mf"]}""";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.Tier.Should().Be("premium");
        config.PrintProfiles.Should().Equal("./a.3mf", "./b.3mf");
    }

    [Fact]
    public void Deserialize_WithNoFields_UsesDefaults()
    {
        var json = "{}";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.Tier.Should().Be("free");
        config.PrintProfiles.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithNoPrintProfiles_DefaultsToEmpty()
    {
        var json = """{"tier":"free"}""";
        var config = JsonSerializer.Deserialize<PlatformConfig>(json);
        config!.PrintProfiles.Should().BeEmpty();
    }
}

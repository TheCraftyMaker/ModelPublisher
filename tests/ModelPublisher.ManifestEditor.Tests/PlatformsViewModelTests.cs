// tests/ModelPublisher.ManifestEditor.Tests/PlatformsViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;
using Xunit;

namespace ModelPublisher.ManifestEditor.Tests;

public class PlatformsViewModelTests
{
    [Fact]
    public void Constructor_CreatesAllSixPlatforms()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Select(e => e.PlatformKey)
            .Should().Equal("printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon");
    }

    [Fact]
    public void AllDisabledByDefault()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Should().AllSatisfy(e => e.IsEnabled.Should().BeFalse());
    }

    [Fact]
    public void IsPatreon_TrueOnlyForPatreon()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.Where(e => e.IsPatreon).Should().ContainSingle(e => e.PlatformKey == "patreon");
        vm.Entries.Where(e => !e.IsPatreon).Should().HaveCount(5);
    }

    [Fact]
    public void TierOptions_ContainsFreeAndPremium()
    {
        var vm = new PlatformsViewModel();
        vm.Entries[0].TierOptions.Should().Equal("free", "premium");
    }

    [Fact]
    public void LoadFrom_SetsEnabledAndConfigFields()
    {
        var vm = new PlatformsViewModel();
        vm.LoadFrom(
        [
            new Models.PlatformState { PlatformKey = "printables",  IsEnabled = true,  Tier = "premium" },
            new Models.PlatformState { PlatformKey = "makerworld",  IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "cults3d",     IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "thangs",      IsEnabled = false, Tier = "free" },
            new Models.PlatformState { PlatformKey = "makeronline", IsEnabled = false, Tier = "free" },
            new Models.PlatformState
            {
                PlatformKey = "patreon", IsEnabled = true,
                Tier = "free", FreePost = false, AccessTierId = "tier_abc"
            },
        ], manifestDir: "/some/dir");

        var printables = vm.Entries.First(e => e.PlatformKey == "printables");
        printables.IsEnabled.Should().BeTrue();
        printables.Tier.Should().Be("premium");

        var patreon = vm.Entries.First(e => e.PlatformKey == "patreon");
        patreon.IsEnabled.Should().BeTrue();
        patreon.FreePost.Should().BeFalse();
        patreon.AccessTierId.Should().Be("tier_abc");
    }

    [Fact]
    public void ToPlatformStates_OnlyReturnsAllSix_WithEnabledFlag()
    {
        var vm = new PlatformsViewModel();
        vm.Entries.First(e => e.PlatformKey == "thangs").IsEnabled = true;

        var states = vm.ToPlatformStates();
        states.Should().HaveCount(6);
        states.First(s => s.PlatformKey == "thangs").IsEnabled.Should().BeTrue();
        states.First(s => s.PlatformKey == "printables").IsEnabled.Should().BeFalse();
    }
}

// tests/ModelPublisher.ManifestEditor.Tests/ManifestEditorStateTests.cs
using System.Text.Json;
using FluentAssertions;
using ModelPublisher.Core.Models;
using ModelPublisher.ManifestEditor.Models;
using Xunit;

namespace ModelPublisher.ManifestEditor.Tests;

public class ManifestEditorStateTests
{
    private static ReleaseManifest MakeManifest(string dir)
    {
        var json = $$"""
        {
          "title": "My Model",
          "description": "# Hello",
          "tags": ["tag1", "tag2"],
          "license": "CC-BY-SA-4.0",
          "files": {
            "models": ["./model.3mf"],
            "photos": ["./cover.jpg", "./detail.jpg"],
            "cover": "./cover.jpg"
          },
          "platforms": {
            "printables": { "tier": "premium", "print_profiles": ["./profile.3mf"] },
            "patreon": { "tier": "free", "free_post": false, "access_tier_id": "tier_abc" }
          }
        }
        """;
        var manifest = JsonSerializer.Deserialize<ReleaseManifest>(json)!;
        manifest.ManifestDirectory = dir;
        return manifest;
    }

    [Fact]
    public void FromManifest_ReadsBasicFields()
    {
        var dir = "C:/models/mymodel";
        var manifest = MakeManifest(dir);

        var state = ManifestEditorState.FromManifest(manifest);

        state.Title.Should().Be("My Model");
        state.Description.Should().Be("# Hello");
        state.Tags.Should().Equal("tag1", "tag2");
        state.License.Should().Be("CC-BY-SA-4.0");
        state.ManifestDirectory.Should().Be(dir);
    }

    [Fact]
    public void FromManifest_ResolvesFilePathsToAbsolute()
    {
        var dir = "C:/models/mymodel";
        var manifest = MakeManifest(dir);

        var state = ManifestEditorState.FromManifest(manifest);

        state.ModelFiles.Should().Equal("C:/models/mymodel/model.3mf".Replace('/', Path.DirectorySeparatorChar));
        state.Photos.Should().HaveCount(2);
        state.Cover.Should().Be("C:/models/mymodel/cover.jpg".Replace('/', Path.DirectorySeparatorChar));
    }

    [Fact]
    public void FromManifest_PopulatesAllSixPlatforms()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);

        state.Platforms.Should().HaveCount(6);
        state.Platforms.Select(p => p.PlatformKey).Should()
            .Equal("printables", "makerworld", "cults3d", "thangs", "makeronline", "patreon");
    }

    [Fact]
    public void FromManifest_MarksEnabledPlatformsCorrectly()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);

        state.Platforms.First(p => p.PlatformKey == "printables").IsEnabled.Should().BeTrue();
        state.Platforms.First(p => p.PlatformKey == "makerworld").IsEnabled.Should().BeFalse();
        state.Platforms.First(p => p.PlatformKey == "patreon").IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void FromManifest_ReadsPatreonFields()
    {
        var manifest = MakeManifest("C:/models/x");
        var state = ManifestEditorState.FromManifest(manifest);
        var patreon = state.Platforms.First(p => p.PlatformKey == "patreon");

        patreon.FreePost.Should().BeFalse();
        patreon.AccessTierId.Should().Be("tier_abc");
    }

    [Fact]
    public void ToJson_RoundTrips_BasicFields()
    {
        var dir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var state = new ManifestEditorState
        {
            Title = "Round Trip",
            Description = "desc",
            Tags = ["a", "b"],
            License = "MIT",
            ManifestDirectory = dir,
        };

        var json = state.ToJson();
        var doc = JsonDocument.Parse(json).RootElement;

        doc.GetProperty("title").GetString().Should().Be("Round Trip");
        doc.GetProperty("license").GetString().Should().Be("MIT");
        doc.GetProperty("tags").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("a", "b");
    }

    [Fact]
    public void ToJson_WritesRelativeFilePaths()
    {
        var dir = Path.Combine(Path.GetTempPath(), "manifesttest");
        var state = new ManifestEditorState
        {
            Title = "T",
            ManifestDirectory = dir,
            ModelFiles = [Path.Combine(dir, "model.3mf")],
            Photos = [Path.Combine(dir, "cover.jpg")],
        };

        var json = state.ToJson();
        var doc = JsonDocument.Parse(json).RootElement;
        var modelPath = doc.GetProperty("files").GetProperty("models")[0].GetString()!;

        modelPath.Should().StartWith("./");
        modelPath.Should().NotContain("\\");
    }

    [Fact]
    public void ToJson_OnlyIncludesEnabledPlatforms()
    {
        var dir = Path.GetTempPath();
        var state = new ManifestEditorState
        {
            Title = "T",
            ManifestDirectory = dir,
            Platforms =
            [
                new PlatformState { PlatformKey = "printables", IsEnabled = true, Tier = "free" },
                new PlatformState { PlatformKey = "makerworld", IsEnabled = false, Tier = "free" },
            ]
        };

        var json = state.ToJson();
        var platforms = JsonDocument.Parse(json).RootElement.GetProperty("platforms");

        platforms.TryGetProperty("printables", out _).Should().BeTrue();
        platforms.TryGetProperty("makerworld", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_OmitsCoverWhenNull()
    {
        var dir = Path.GetTempPath();
        var state = new ManifestEditorState { Title = "T", ManifestDirectory = dir };

        var json = state.ToJson();
        var files = JsonDocument.Parse(json).RootElement.GetProperty("files");

        if (files.TryGetProperty("cover", out var cover))
            cover.ValueKind.Should().Be(JsonValueKind.Null);
    }
}

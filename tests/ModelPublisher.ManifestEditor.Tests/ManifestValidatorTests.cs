// tests/ModelPublisher.ManifestEditor.Tests/ManifestValidatorTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.Models;
using Xunit;

namespace ModelPublisher.ManifestEditor.Tests;

public class ManifestValidatorTests
{
    private static readonly string _existingFile = typeof(ManifestValidatorTests).Assembly.Location;
    private static readonly string _existingDir = Path.GetDirectoryName(typeof(ManifestValidatorTests).Assembly.Location)!;

    private static ManifestEditorState ValidState()
    {
        return new ManifestEditorState
        {
            Title = "Valid Title",
            ManifestDirectory = _existingDir,
            ModelFiles = [_existingFile],
            // photos not required for basic validity
        };
    }

    [Fact]
    public void EmptyTitle_ReturnsError()
    {
        var state = ValidState();
        state.Title = "";
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public void WhitespaceTitle_ReturnsError()
    {
        var state = ValidState();
        state.Title = "   ";
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public void NoModelFiles_ReturnsError()
    {
        var state = ValidState();
        state.ModelFiles.Clear();
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("model"));
    }

    [Fact]
    public void MissingModelFile_ReturnsError()
    {
        var state = ValidState();
        state.ModelFiles = ["/does/not/exist/model.3mf"];
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("model.3mf"));
    }

    [Fact]
    public void MissingPhotoFile_ReturnsError()
    {
        var state = ValidState();
        state.Photos = ["/does/not/exist/photo.jpg"];
        ManifestValidator.Validate(state).Should().Contain(e => e.Contains("photo.jpg"));
    }

    [Fact]
    public void PatreonEnabled_FreePostFalse_NoAccessTierId_ReturnsError()
    {
        var state = ValidState();
        state.Platforms.Add(new PlatformState
        {
            PlatformKey = "patreon",
            IsEnabled = true,
            FreePost = false,
            AccessTierId = "",
        });
        ManifestValidator.Validate(state).Should()
            .Contain(e => e.Contains("access_tier_id"));
    }

    [Fact]
    public void PatreonEnabled_FreePostTrue_NoAccessTierId_IsValid()
    {
        var state = ValidState();
        state.Platforms.Add(new PlatformState
        {
            PlatformKey = "patreon",
            IsEnabled = true,
            FreePost = true,
            AccessTierId = "",
        });
        ManifestValidator.Validate(state).Should().BeEmpty();
    }

    [Fact]
    public void AllValid_ReturnsEmpty()
    {
        var dir = Path.GetTempPath();
        var modelPath = Path.Combine(dir, "model.3mf");
        File.WriteAllText(modelPath, "dummy");

        var state = new ManifestEditorState
        {
            Title = "Good Model",
            ManifestDirectory = dir,
            ModelFiles = [modelPath],
        };

        try
        {
            ManifestValidator.Validate(state).Should().BeEmpty();
        }
        finally
        {
            File.Delete(modelPath);
        }
    }
}

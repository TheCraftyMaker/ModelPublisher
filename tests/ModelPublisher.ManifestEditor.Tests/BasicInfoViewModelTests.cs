// tests/ModelPublisher.ManifestEditor.Tests/BasicInfoViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;
using Xunit;

namespace ModelPublisher.ManifestEditor.Tests;

public class BasicInfoViewModelTests
{
    [Fact]
    public void AddTag_AppendsTrimmedTag()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("  newtag  ");
        vm.Tags.Should().ContainSingle().Which.Should().Be("newtag");
    }

    [Fact]
    public void AddTag_EmptyString_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_Whitespace_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("   ");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_Duplicate_DoesNothing()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("foo");
        vm.AddTag("foo");
        vm.Tags.Should().ContainSingle();
    }

    [Fact]
    public void RemoveTag_RemovesExistingTag()
    {
        var vm = new BasicInfoViewModel();
        vm.AddTag("foo");
        vm.RemoveTag("foo");
        vm.Tags.Should().BeEmpty();
    }

    [Fact]
    public void LoadFrom_SetsAllFields()
    {
        var vm = new BasicInfoViewModel();
        vm.LoadFrom("My Model", ["tag1", "tag2"], "MIT");
        vm.Title.Should().Be("My Model");
        vm.Tags.Should().Equal("tag1", "tag2");
        vm.License.Should().Be("MIT");
    }
}

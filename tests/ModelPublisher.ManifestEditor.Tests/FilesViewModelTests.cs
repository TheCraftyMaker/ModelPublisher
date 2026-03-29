// tests/ModelPublisher.ManifestEditor.Tests/FilesViewModelTests.cs
using FluentAssertions;
using ModelPublisher.ManifestEditor.ViewModels;
using Xunit;

namespace ModelPublisher.ManifestEditor.Tests;

public class FilesViewModelTests
{
    [Fact]
    public void AddModelFile_AppendsToList()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a/model.3mf");
        vm.ModelFiles.Should().ContainSingle(e => e.AbsolutePath == "/a/model.3mf");
    }

    [Fact]
    public void RemoveModelFile_RemovesFromList()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a/model.3mf");
        var entry = vm.ModelFiles[0];
        vm.RemoveModelFile(entry);
        vm.ModelFiles.Should().BeEmpty();
    }

    [Fact]
    public void MoveUpModelFile_MovesItemUp()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var b = vm.ModelFiles[1];
        vm.MoveUpModelFile(b);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/b.3mf");
        vm.ModelFiles[1].AbsolutePath.Should().Be("/a.3mf");
    }

    [Fact]
    public void MoveUpModelFile_DoesNothing_WhenFirst()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var a = vm.ModelFiles[0];
        vm.MoveUpModelFile(a);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/a.3mf");
    }

    [Fact]
    public void MoveDownModelFile_MovesItemDown()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var a = vm.ModelFiles[0];
        vm.MoveDownModelFile(a);
        vm.ModelFiles[0].AbsolutePath.Should().Be("/b.3mf");
    }

    [Fact]
    public void MoveDownModelFile_DoesNothing_WhenLast()
    {
        var vm = new FilesViewModel();
        vm.AddModelFile("/a.3mf");
        vm.AddModelFile("/b.3mf");
        var b = vm.ModelFiles[1];
        vm.MoveDownModelFile(b);
        vm.ModelFiles[1].AbsolutePath.Should().Be("/b.3mf");
    }

    [Fact]
    public void CoverOptions_IncludesNoneAndPhotos()
    {
        var vm = new FilesViewModel();
        vm.AddPhoto("/a/cover.jpg");
        vm.CoverOptions.Should().HaveCount(2);
        vm.CoverOptions[0].Should().BeNull();
        vm.CoverOptions[1].Should().Be("/a/cover.jpg");
    }

    [Fact]
    public void LoadFrom_SetsAllFields()
    {
        var vm = new FilesViewModel();
        vm.LoadFrom(
            modelFiles: ["/a/model.3mf"],
            photos: ["/a/cover.jpg"],
            cover: "/a/cover.jpg",
            manifestDir: "/a");

        vm.ModelFiles.Should().ContainSingle(e => e.AbsolutePath == "/a/model.3mf");
        vm.Photos.Should().ContainSingle(e => e.AbsolutePath == "/a/cover.jpg");
        vm.SelectedCover.Should().Be("/a/cover.jpg");
    }
}

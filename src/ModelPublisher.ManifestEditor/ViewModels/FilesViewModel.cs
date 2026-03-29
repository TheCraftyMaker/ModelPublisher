using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModelPublisher.ManifestEditor.Services;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class FilesViewModel : ObservableObject, ISectionViewModel
{
    private readonly IFileDialogService _dialogs;

    public string SectionName => "Files";

    public ObservableCollection<FileEntryViewModel> ModelFiles { get; } = [];
    public ObservableCollection<FileEntryViewModel> Photos { get; } = [];

    [ObservableProperty]
    private string? _selectedCover;

    public ObservableCollection<string?> CoverOptions { get; } = [];

    public FilesViewModel() : this(new NullFileDialogService()) { }

    public FilesViewModel(IFileDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task AddModelFileAsync()
    {
        var paths = await _dialogs.OpenFilesAsync("Select model files", ".3mf", ".stl", ".obj", ".zip");
        foreach (var p in paths) AddModelFile(p);
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var paths = await _dialogs.OpenFilesAsync("Select photos", ".jpg", ".jpeg", ".png", ".webp");
        foreach (var p in paths) AddPhoto(p);
    }

    public void AddModelFile(string absolutePath) =>
        ModelFiles.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));

    [RelayCommand]
    public void RemoveModelFile(FileEntryViewModel item) => ModelFiles.Remove(item);

    [RelayCommand]
    public void MoveUpModelFile(FileEntryViewModel item)
    {
        var i = ModelFiles.IndexOf(item);
        if (i > 0) ModelFiles.Move(i, i - 1);
    }

    [RelayCommand]
    public void MoveDownModelFile(FileEntryViewModel item)
    {
        var i = ModelFiles.IndexOf(item);
        if (i >= 0 && i < ModelFiles.Count - 1) ModelFiles.Move(i, i + 1);
    }

    public void AddPhoto(string absolutePath)
    {
        Photos.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));
        RebuildCoverOptions();
    }

    [RelayCommand]
    private void RemovePhoto(FileEntryViewModel item)
    {
        if (SelectedCover == item.AbsolutePath) SelectedCover = null;
        Photos.Remove(item);
        RebuildCoverOptions();
    }

    [RelayCommand]
    public void MoveUpPhoto(FileEntryViewModel item)
    {
        var i = Photos.IndexOf(item);
        if (i > 0) Photos.Move(i, i - 1);
    }

    [RelayCommand]
    public void MoveDownPhoto(FileEntryViewModel item)
    {
        var i = Photos.IndexOf(item);
        if (i >= 0 && i < Photos.Count - 1) Photos.Move(i, i + 1);
    }

    public void LoadFrom(
        IEnumerable<string> modelFiles,
        IEnumerable<string> photos,
        string? cover,
        string manifestDir)
    {
        ModelFiles.Clear();
        Photos.Clear();

        foreach (var f in modelFiles)
            ModelFiles.Add(new FileEntryViewModel(f, !File.Exists(f)));

        foreach (var p in photos)
            Photos.Add(new FileEntryViewModel(p, !File.Exists(p)));

        RebuildCoverOptions();
        SelectedCover = cover;
    }

    public void Clear()
    {
        ModelFiles.Clear();
        Photos.Clear();
        CoverOptions.Clear();
        CoverOptions.Add(null);
        SelectedCover = null;
    }

    private void RebuildCoverOptions()
    {
        CoverOptions.Clear();
        CoverOptions.Add(null);
        foreach (var p in Photos)
            CoverOptions.Add(p.AbsolutePath);
    }
}

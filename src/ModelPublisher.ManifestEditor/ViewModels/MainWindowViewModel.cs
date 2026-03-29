using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModelPublisher.Core.Models;
using ModelPublisher.ManifestEditor.Models;
using ModelPublisher.ManifestEditor.Services;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileDialogService _dialogs;

    public BasicInfoViewModel   BasicInfo   { get; } = new();
    public FilesViewModel       Files       { get; }
    public DescriptionViewModel Description { get; } = new();
    public PlatformsViewModel   Platforms   { get; }

    public ObservableCollection<ISectionViewModel> Sections { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private ISectionViewModel _activeSection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _manifestDirectory = "";

    public string WindowTitle =>
        $"Manifest Editor{(string.IsNullOrEmpty(ManifestDirectory) ? "" : $" - {Path.GetFileName(ManifestDirectory)}")}{(IsDirty ? " *" : "")}";

    public bool CanSave => !string.IsNullOrEmpty(ManifestDirectory);

    public MainWindowViewModel(IFileDialogService dialogs)
    {
        _dialogs = dialogs;
        Files = new FilesViewModel(dialogs);
        Platforms = new PlatformsViewModel(dialogs);
        Sections = [BasicInfo, Files, Description, Platforms];
        _activeSection = BasicInfo;
        SubscribeDirtyTracking();
    }

    private void SubscribeDirtyTracking()
    {
        BasicInfo.PropertyChanged += (_, _) => IsDirty = true;
        BasicInfo.Tags.CollectionChanged += (_, _) => IsDirty = true;
        Files.ModelFiles.CollectionChanged += (_, _) => IsDirty = true;
        Files.Photos.CollectionChanged += (_, _) => IsDirty = true;
        Files.PropertyChanged += (_, _) => IsDirty = true;
        Description.PropertyChanged += (_, _) => IsDirty = true;
        foreach (var entry in Platforms.Entries)
        {
            entry.PropertyChanged += (_, _) => IsDirty = true;
            entry.PrintProfiles.CollectionChanged += (_, _) => IsDirty = true;
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (IsDirty)
        {
            var save = await _dialogs.ConfirmUnsavedChangesAsync();
            if (save) await SaveAsync();
        }

        var path = await _dialogs.OpenManifestLocationAsync();
        if (path is null) return;

        string manifestPath;
        string dir;

        if (Directory.Exists(path))
        {
            dir = path;
            manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                LoadBlankState(dir);
                return;
            }
        }
        else
        {
            manifestPath = path;
            dir = Path.GetDirectoryName(path)!;
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<ReleaseManifest>(json);
            if (manifest is null) throw new JsonException("Deserialized to null.");
            manifest.ManifestDirectory = dir;
            LoadFromManifest(manifest);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Could not open manifest", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var state = BuildState();
        var errors = ManifestValidator.Validate(state);
        if (errors.Count > 0)
        {
            await _dialogs.ShowValidationErrorsAsync(errors);
            return;
        }

        try
        {
            var json = state.ToJson();
            var path = Path.Combine(ManifestDirectory, "manifest.json");
            await File.WriteAllTextAsync(path, json);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Save failed", ex.Message);
        }
    }

    public async Task<bool> ConfirmAndSaveBeforeCloseAsync()
        => await _dialogs.ConfirmUnsavedChangesAsync();

    private void LoadFromManifest(ReleaseManifest manifest)
    {
        var state = ManifestEditorState.FromManifest(manifest);
        ManifestDirectory = state.ManifestDirectory;
        BasicInfo.LoadFrom(state.Title, state.Tags, state.License);
        Files.LoadFrom(state.ModelFiles, state.Photos, state.Cover, state.ManifestDirectory);
        Description.Text = state.Description;
        Platforms.LoadFrom(state.Platforms, state.ManifestDirectory);
        IsDirty = false;
    }

    private void LoadBlankState(string dir)
    {
        ManifestDirectory = dir;
        BasicInfo.Clear();
        Files.Clear();
        Description.Text = "";
        Platforms.Clear();
        IsDirty = false;
    }

    private ManifestEditorState BuildState() => new()
    {
        Title = BasicInfo.Title,
        Description = Description.Text,
        Tags = BasicInfo.Tags.ToList(),
        License = BasicInfo.License,
        ManifestDirectory = ManifestDirectory,
        ModelFiles = Files.ModelFiles.Select(e => e.AbsolutePath).ToList(),
        Photos = Files.Photos.Select(e => e.AbsolutePath).ToList(),
        Cover = Files.SelectedCover,
        Platforms = Platforms.ToPlatformStates(),
    };
}

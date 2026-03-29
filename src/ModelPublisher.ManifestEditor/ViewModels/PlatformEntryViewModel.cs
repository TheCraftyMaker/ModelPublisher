using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModelPublisher.ManifestEditor.Models;
using ModelPublisher.ManifestEditor.Services;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class PlatformEntryViewModel : ObservableObject
{
    private readonly IFileDialogService _dialogs;

    public string[] TierOptions { get; } = ["free", "premium"];

    public string PlatformKey { get; }
    public string PlatformName { get; }
    public bool IsPatreon => PlatformKey == "patreon";

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _tier = "free";
    [ObservableProperty] private bool _freePost = true;
    [ObservableProperty] private string _accessTierId = "";

    public ObservableCollection<FileEntryViewModel> PrintProfiles { get; } = [];

    private static readonly Dictionary<string, string> PlatformNames = new()
    {
        ["printables"]  = "Printables",
        ["makerworld"]  = "MakerWorld",
        ["cults3d"]     = "Cults3D",
        ["thangs"]      = "Thangs",
        ["makeronline"] = "MakerOnline",
        ["patreon"]     = "Patreon",
    };

    public PlatformEntryViewModel(string platformKey)
        : this(platformKey, new NullFileDialogService()) { }

    public PlatformEntryViewModel(string platformKey, IFileDialogService dialogs)
    {
        PlatformKey = platformKey;
        PlatformName = PlatformNames.GetValueOrDefault(platformKey, platformKey);
        _dialogs = dialogs;
    }

    public void LoadFrom(PlatformState state, string manifestDir)
    {
        IsEnabled = state.IsEnabled;
        Tier = state.Tier;
        FreePost = state.FreePost ?? true;
        AccessTierId = state.AccessTierId ?? "";
        PrintProfiles.Clear();
        foreach (var p in state.PrintProfiles)
            PrintProfiles.Add(new FileEntryViewModel(p, !File.Exists(p)));
    }

    public PlatformState ToState() => new()
    {
        PlatformKey = PlatformKey,
        IsEnabled = IsEnabled,
        Tier = Tier,
        PrintProfiles = PrintProfiles.Select(p => p.AbsolutePath).ToList(),
        FreePost = FreePost,
        AccessTierId = AccessTierId,
    };

    public void AddPrintProfile(string absolutePath) =>
        PrintProfiles.Add(new FileEntryViewModel(absolutePath, !File.Exists(absolutePath)));

    [RelayCommand]
    public void RemovePrintProfile(FileEntryViewModel item) => PrintProfiles.Remove(item);

    [RelayCommand]
    private async Task AddPrintProfileAsync()
    {
        var paths = await _dialogs.OpenFilesAsync("Select print profile", ".3mf");
        foreach (var p in paths) AddPrintProfile(p);
    }
}

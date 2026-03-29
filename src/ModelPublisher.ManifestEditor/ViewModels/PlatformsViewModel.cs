using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ModelPublisher.ManifestEditor.Models;
using ModelPublisher.ManifestEditor.Services;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class PlatformsViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Platforms";

    public ObservableCollection<PlatformEntryViewModel> Entries { get; } = [];

    public PlatformsViewModel() : this(new NullFileDialogService()) { }

    public PlatformsViewModel(IFileDialogService dialogs)
    {
        foreach (var key in ManifestEditorState.AllPlatformKeys)
            Entries.Add(new PlatformEntryViewModel(key, dialogs));
    }

    public void LoadFrom(IEnumerable<PlatformState> states, string manifestDir)
    {
        var stateMap = states.ToDictionary(s => s.PlatformKey);
        foreach (var entry in Entries)
        {
            if (stateMap.TryGetValue(entry.PlatformKey, out var state))
                entry.LoadFrom(state, manifestDir);
        }
    }

    public List<PlatformState> ToPlatformStates() =>
        Entries.Select(e => e.ToState()).ToList();

    public void Clear()
    {
        foreach (var e in Entries)
            e.LoadFrom(new PlatformState { PlatformKey = e.PlatformKey }, manifestDir: "");
    }
}

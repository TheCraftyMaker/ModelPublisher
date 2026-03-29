using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class BasicInfoViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Basic Info";

    public string[] LicenseOptions { get; } =
    [
        "CC-BY-4.0", "CC-BY-SA-4.0", "CC-BY-NC-4.0", "CC-BY-NC-SA-4.0",
        "CC0-1.0", "MIT", "GPL-3.0-only",
    ];

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _license = "CC-BY-4.0";

    [ObservableProperty]
    private string _newTagText = "";

    public ObservableCollection<string> Tags { get; } = [];

    public void AddTag(string tag)
    {
        tag = tag.Trim();
        if (string.IsNullOrEmpty(tag)) return;
        if (Tags.Contains(tag)) return;
        Tags.Add(tag);
    }

    [RelayCommand]
    private void SubmitNewTag()
    {
        AddTag(NewTagText);
        NewTagText = "";
    }

    [RelayCommand]
    public void RemoveTag(string tag) => Tags.Remove(tag);

    public void LoadFrom(string title, IEnumerable<string> tags, string license)
    {
        Title = title;
        License = license;
        Tags.Clear();
        foreach (var t in tags) Tags.Add(t);
    }

    public void Clear()
    {
        Title = "";
        License = "CC-BY-4.0";
        Tags.Clear();
        NewTagText = "";
    }
}

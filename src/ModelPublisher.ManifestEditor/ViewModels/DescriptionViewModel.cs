using CommunityToolkit.Mvvm.ComponentModel;

namespace ModelPublisher.ManifestEditor.ViewModels;

public partial class DescriptionViewModel : ObservableObject, ISectionViewModel
{
    public string SectionName => "Description";

    [ObservableProperty]
    private string _text = "";
}

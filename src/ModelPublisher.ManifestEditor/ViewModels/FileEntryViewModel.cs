namespace ModelPublisher.ManifestEditor.ViewModels;

public class FileEntryViewModel
{
    public string AbsolutePath { get; }
    public bool IsMissing { get; }
    public string DisplayName => Path.GetFileName(AbsolutePath);

    public FileEntryViewModel(string absolutePath, bool isMissing = false)
    {
        AbsolutePath = absolutePath;
        IsMissing = isMissing;
    }
}
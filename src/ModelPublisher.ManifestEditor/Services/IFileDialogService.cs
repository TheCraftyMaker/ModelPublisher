namespace ModelPublisher.ManifestEditor.Services;

public interface IFileDialogService
{
    /// <summary>Opens a folder or manifest.json file picker. Returns the selected path or null.</summary>
    Task<string?> OpenManifestLocationAsync();

    /// <summary>Opens a file picker for model/photo/profile files. Returns selected absolute paths.</summary>
    Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions);

    Task ShowErrorAsync(string title, string message);

    /// <summary>Returns true if the user clicks "Save".</summary>
    Task<bool> ConfirmUnsavedChangesAsync();

    /// <summary>Shows validation errors.</summary>
    Task ShowValidationErrorsAsync(IReadOnlyList<string> errors);
}

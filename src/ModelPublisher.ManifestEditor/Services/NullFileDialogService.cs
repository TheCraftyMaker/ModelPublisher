namespace ModelPublisher.ManifestEditor.Services;

internal class NullFileDialogService : IFileDialogService
{
    public Task<string?> OpenManifestLocationAsync() => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions)
        => Task.FromResult<IReadOnlyList<string>>([]);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task<bool> ConfirmUnsavedChangesAsync() => Task.FromResult(false);
    public Task ShowValidationErrorsAsync(IReadOnlyList<string> errors) => Task.CompletedTask;
}

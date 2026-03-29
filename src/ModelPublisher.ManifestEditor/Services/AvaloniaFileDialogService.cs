using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ModelPublisher.ManifestEditor.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Window _owner;

    public AvaloniaFileDialogService(Window owner) => _owner = owner;

    public async Task<string?> OpenManifestLocationAsync()
    {
        var folderResult = await _owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select model folder or manifest.json" });

        if (folderResult.Count > 0)
            return folderResult[0].Path.LocalPath;

        return null;
    }

    public async Task<IReadOnlyList<string>> OpenFilesAsync(string title, params string[] extensions)
    {
        var patterns = extensions.Select(e => e.StartsWith('.') ? $"*{e}" : e).ToArray();
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Files") { Patterns = patterns },
            ]
        });
        return files.Select(f => f.Path.LocalPath).ToArray();
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 160,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    }
                }
            }
        };
        ((Button)((StackPanel)dialog.Content!).Children[1]).Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(_owner);
    }

    public async Task<bool> ConfirmUnsavedChangesAsync()
    {
        var result = false;
        var dialog = new Window
        {
            Title = "Unsaved changes",
            Width = 360,
            Height = 150,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = "You have unsaved changes. Save before opening?" },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            new Button { Content = "Don't Save" },
                            new Button { Content = "Save" },
                        }
                    }
                }
            }
        };
        var buttons = (StackPanel)((StackPanel)dialog.Content!).Children[1];
        ((Button)buttons.Children[0]).Click += (_, _) => { result = false; dialog.Close(); };
        ((Button)buttons.Children[1]).Click += (_, _) => { result = true; dialog.Close(); };
        await dialog.ShowDialog(_owner);
        return result;
    }

    public async Task ShowValidationErrorsAsync(IReadOnlyList<string> errors)
    {
        var errorText = string.Join("\n", errors.Select(e => $"- {e}"));
        await ShowErrorAsync("Cannot save - validation errors", errorText);
    }
}

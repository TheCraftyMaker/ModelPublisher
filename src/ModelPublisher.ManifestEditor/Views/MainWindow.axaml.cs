using Avalonia.Controls;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Views;

public partial class MainWindow : Window
{
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.IsDirty)
        {
            e.Cancel = true;
            var save = await vm.ConfirmAndSaveBeforeCloseAsync();
            if (save) await vm.SaveCommand.ExecuteAsync(null);
            Close();
        }
        base.OnClosing(e);
    }
}

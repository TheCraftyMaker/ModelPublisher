using Avalonia.Controls;
using Avalonia.Input;
using ModelPublisher.ManifestEditor.ViewModels;

namespace ModelPublisher.ManifestEditor.Views;

public partial class BasicInfoView : UserControl
{
    private void TagInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is BasicInfoViewModel vm)
        {
            vm.SubmitNewTagCommand.Execute(null);
            e.Handled = true;
        }
    }
}

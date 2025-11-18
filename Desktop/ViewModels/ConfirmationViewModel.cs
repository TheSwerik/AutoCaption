using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Views;

namespace Desktop.ViewModels;

public partial class ConfirmationViewModel : ViewModelBase
{
    [ObservableProperty] private string _message = "Are you sure?";
    [ObservableProperty] private string _title = "Are you sure?";

    public ConfirmationViewModel()
    {
    }

    public ConfirmationViewModel(string title, string message)
    {
        _title = title;
        _message = message;
    }

    [RelayCommand]
    private static void Yes()
    {
        var window = App.Windows.First(w => w is ConfirmationWindow);
        window.Close(true);
    }

    [RelayCommand]
    private void No()
    {
        Cancel();
    }

    private static void Cancel()
    {
        var window = App.Windows.First(w => w is ConfirmationWindow);
        window.Close(false);
    }
}
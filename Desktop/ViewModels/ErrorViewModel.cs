using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Views.Modals;

namespace Desktop.ViewModels;

public partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty] private string _message = "Are you sure?";

    public ErrorViewModel()
    {
    }

    public ErrorViewModel(string message)
    {
        _message = message;
    }

    [RelayCommand]
    private static void Ok()
    {
        Cancel();
    }

    private static void Cancel()
    {
        var window = App.Windows.First(w => w is ErrorWindow);
        window.Close(false);
    }
}
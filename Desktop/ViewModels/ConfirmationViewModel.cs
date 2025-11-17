using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

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
    private void Yes()
    {
        WeakReferenceMessenger.Default.Send(new CloseConfirmationWindowMessage(true));
    }

    [RelayCommand]
    private void No()
    {
        Cancel();
    }

    private static void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new CloseConfirmationWindowMessage(false));
    }
}
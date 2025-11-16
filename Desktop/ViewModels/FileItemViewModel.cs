using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Desktop.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _fileString = "";
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isInProgress;
    [ObservableProperty] private double _progress;

    [RelayCommand]
    private async Task OpenEdit()
    {
        IsInProgress = !IsInProgress;
        IsCompleted = !IsCompleted;
    }

    [RelayCommand]
    private async Task Delete()
    {
        WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(FileString));
    }
}
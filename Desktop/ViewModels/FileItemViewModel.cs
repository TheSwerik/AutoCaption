using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _filesString = "";
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
        IsInProgress = !IsInProgress;
        IsCompleted = !IsCompleted;
    }
}
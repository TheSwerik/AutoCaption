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
    private void OpenEdit()
    {
        WeakReferenceMessenger.Default.Send(new EditInputFileMessage(this));
    }

    [RelayCommand]
    private void Delete()
    {
        if (IsInProgress)
            //TODO are you sure?
            return;

        WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(FileString));
    }
}
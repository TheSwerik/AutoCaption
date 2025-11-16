using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Desktop.ViewModels;

public partial class AddViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task Add()
    {
         WeakReferenceMessenger.Default.Send(new AddInputFilesMessage());
    }
}
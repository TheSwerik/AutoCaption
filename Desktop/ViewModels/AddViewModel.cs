using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class AddViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task Add()
    {
        Console.WriteLine("add");
    }
}
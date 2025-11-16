using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class OutputSettingsViewModel : ViewModelBase
{
    public bool DoSplitting { get; set; } = true;
    public string OutputLocation { get; set; } = "";

    [RelayCommand]
    private async Task SelectOutputLocation()
    {
    }
}
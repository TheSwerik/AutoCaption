using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Desktop.ViewModels;

public partial class FileItemSettingsViewModel : ViewModelBase
{
    private FileItemViewModel _file;

    public FileItemSettingsViewModel()
    {
        _file = new FileItemViewModel();
    }

    public FileItemSettingsViewModel(FileItemViewModel file)
    {
        _file = file;
    }

    public bool DoSplitting { get; set; } = true;
    public string OutputLocation { get; set; } = "";

    [RelayCommand]
    private async Task SelectOutputLocation()
    {
    }
}
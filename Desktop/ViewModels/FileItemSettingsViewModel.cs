using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Views;

namespace Desktop.ViewModels;

public partial class FileItemSettingsViewModel : ViewModelBase
{
    private static readonly FolderPickerOpenOptions OutputOptions = new()
    {
        AllowMultiple = false,
        Title = "Output Location"
    };

    private readonly FileItemViewModel _original;
    [ObservableProperty] private FileItemViewModel _file;

    [ObservableProperty] private CultureInfo _language = CultureInfo.GetCultureInfo("en-US");

    [ObservableProperty] private ObservableCollection<CultureInfo> _languages =
        new(CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(c => c.EnglishName));

    public FileItemSettingsViewModel()
    {
        _original = new FileItemViewModel();
        _file = new FileItemViewModel();
    }

    public FileItemSettingsViewModel(FileItemViewModel file)
    {
        _original = file;
        _file = new FileItemViewModel
        {
            IsInProgress = file.IsInProgress,
            Progress = file.Progress,
            Path = file.Path,
            IsCompleted = file.IsCompleted,
            DoSplitting = file.DoSplitting,
            Language = file.Language,
            OutputLocation = file.OutputLocation
        };
        _language = CultureInfo.GetCultureInfo(file.Language);
    }

    [RelayCommand]
    private async Task SelectOutputLocation()
    {
        var mainWindow = App.Windows.First(w => w is FileItemSettingsWindow);
        var topLevel = TopLevel.GetTopLevel(mainWindow) ?? throw new UnreachableException();
        var output = await topLevel.StorageProvider.OpenFolderPickerAsync(OutputOptions);

        switch (output.Count)
        {
            case 0:
                return;
            case 1:
                File.OutputLocation = output[0].Path.LocalPath;
                break;
            default:
                throw new UnreachableException();
        }
    }

    [RelayCommand]
    private void LanguageChanged()
    {
        File.Language = Language.TwoLetterISOLanguageName;
    }

    [RelayCommand]
    private void Save()
    {
        _original.IsInProgress = File.IsInProgress;
        _original.Progress = File.Progress;
        _original.Path = File.Path.Replace('\\', '/');
        _original.IsCompleted = File.IsCompleted;
        _original.DoSplitting = File.DoSplitting;
        _original.Language = File.Language;
        _original.OutputLocation = File.OutputLocation.Replace('\\', '/');

        var window = App.Windows.First(w => w is FileItemSettingsWindow);
        window.Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        var window = App.Windows.First(w => w is FileItemSettingsWindow);
        window.Close(false);
    }
}
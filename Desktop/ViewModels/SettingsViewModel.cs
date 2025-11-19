using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Services;
using Desktop.Views.Modals;

namespace Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private static readonly FolderPickerOpenOptions OutputOptions = new()
    {
        AllowMultiple = false,
        Title = "Output Location"
    };

    private readonly ConfigService.Configuration _original;
    [ObservableProperty] private CultureInfo _language;

    [ObservableProperty] private ObservableCollection<CultureInfo> _languages =
        new(CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(c => c.EnglishName));

    [ObservableProperty] private ObservableCollection<Loglevel> _logLevels = new(Enum.GetValues<Loglevel>());

    [ObservableProperty] private ObservableCollection<Model> _models = new(Enum.GetValues<Model>());

    [ObservableProperty] private ObservableCollection<OutputFormat> _outputFormats = new(Enum.GetValues<OutputFormat>());

    [ObservableProperty] private ConfigService.Configuration _settings;

    public SettingsViewModel()
    {
        _original = ConfigService.Config;
        _settings = new ConfigService.Configuration
        {
            DoSplitting = _original.DoSplitting,
            Language = _original.Language,
            OutputLocation = _original.OutputLocation,
            UseGpu = _original.UseGpu,
            ChunkSize = _original.ChunkSize,
            Model = _original.Model,
            OutputFormat = _original.OutputFormat,
            LogLevel = _original.LogLevel,
            LogToFile = _original.LogToFile
        };
        _language = CultureInfo.GetCultureInfo(_settings.Language);
    }

    public bool DoSplitting
    {
        get => ConfigService.Config.DoSplitting;
        set
        {
            ConfigService.Config.DoSplitting = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task SelectOutputLocation()
    {
        var window = App.Windows.First(w => w is SettingsWindow);
        var topLevel = TopLevel.GetTopLevel(window) ?? throw new UnreachableException();
        var output = await topLevel.StorageProvider.OpenFolderPickerAsync(OutputOptions);

        if (output.Count != 1) throw new UnreachableException();

        Settings.OutputLocation = output[0].Path.LocalPath;
    }

    [RelayCommand]
    private void LanguageChanged()
    {
        Settings.Language = Language.TwoLetterISOLanguageName;
    }

    [RelayCommand]
    private void Save()
    {
        _original.DoSplitting = Settings.DoSplitting;
        _original.Language = Settings.Language;
        _original.OutputLocation = Settings.OutputLocation;
        _original.ChunkSize = Settings.ChunkSize;
        _original.Model = Settings.Model;
        _original.OutputFormat = Settings.OutputFormat;
        _original.UseGpu = Settings.UseGpu;
        _original.LogLevel = Settings.LogLevel;
        _original.LogToFile = Settings.LogToFile;

        var window = App.Windows.First(w => w is SettingsWindow);
        window.Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        var window = App.Windows.First(w => w is SettingsWindow);
        window.Close(false);
    }
}
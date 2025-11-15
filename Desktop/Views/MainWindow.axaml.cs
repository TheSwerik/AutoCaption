using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Desktop.Services;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        #region Variables, that should be selected per file

        var language = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
        var outputLocation = "\"E:/YouTube/captions/\"";

        #endregion

        var opt = new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = "Input Files"
        };
        var files = await StorageProvider.OpenFilePickerAsync(opt);
        var settings = files.Select(f =>
            new WhisperSettings(f.Path.LocalPath, outputLocation, language.TwoLetterISOLanguageName)).ToArray();
        await WhisperService.Start(settings);
    }
}
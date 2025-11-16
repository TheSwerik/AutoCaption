using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Desktop.Services;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    private CultureInfo[] _cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        #region Variables, that should be selected per file

        var language = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");

        #endregion

        var inputOpt = new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = "Input Files"
        };
        var input = await StorageProvider.OpenFilePickerAsync(inputOpt);

        var outputOpt = new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Output Location"
        };
        var output = await StorageProvider.OpenFolderPickerAsync(outputOpt);

        Console.WriteLine("Language: " + language.TwoLetterISOLanguageName);

        var settings = input.Select(f =>
                new WhisperSettings(f.Path.LocalPath, $"\"{output[0].Path.LocalPath.Replace('\\', '/')}\"",
                    language.TwoLetterISOLanguageName))
            .ToArray();
        await WhisperService.Start(settings);
    }
}
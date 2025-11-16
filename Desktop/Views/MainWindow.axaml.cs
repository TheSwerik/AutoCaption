using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Services;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    private CultureInfo[] _cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
    private static readonly FilePickerOpenOptions inputOptions = new FilePickerOpenOptions
    {
        AllowMultiple = true,
        Title = "Input Files"
    };

    public MainWindow()
    {
        InitializeComponent();

        if(Design.IsDesignMode) return;

        WeakReferenceMessenger.Default.Register<MainWindow, AddInputFilesMessage>(this, static async void (w, m) =>
        {
            var input = await GetTopLevel(w)!.StorageProvider.OpenFilePickerAsync(inputOptions);
            var files = input.Select(f => f.Path.LocalPath);
            var viewModel = w.DataContext as MainWindowViewModel;
            viewModel!.AddFiles(files);
        });

        WeakReferenceMessenger.Default.Register<MainWindow, RemoveInputFileMessage>(this, static async void (w, m) =>
        {
            var viewModel = w.DataContext as MainWindowViewModel;
            viewModel!.RemoveFile(m.Path);
        });
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Services;
using Desktop.Views;
using MediaToolkit;
using MediaToolkit.Model;

namespace Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly FilePickerOpenOptions InputOpt = new()
    {
        AllowMultiple = true,
        Title = "Input Files"
    };

    [ObservableProperty] private ObservableCollection<ViewModelBase> _fileItems = [new AddViewModel()];

    [ObservableProperty] private string _filesString = "";
    [ObservableProperty] private bool _isInProgress;
    [ObservableProperty] private double _progress = 50;
    private List<string> Files { get; set; } = [];

    [RelayCommand]
    private async Task SelectInput()
    {
        var mainWindow = App.Windows.First(w => w is MainWindow);
        var topLevel = TopLevel.GetTopLevel(mainWindow) ?? throw new UnreachableException();
        var input = await topLevel.StorageProvider.OpenFilePickerAsync(InputOpt);

        Files = input.Select(f => f.Path.LocalPath).ToList();
        FileItems = new ObservableCollection<ViewModelBase>(input.Select(f => new FileItemViewModel
        {
            FilesString = f.Path.LocalPath,
            IsInProgress = false,
            Progress = 0
        }).ToList()) { new AddViewModel() };
        FilesString = string.Join('\n', Files);
    }

    [RelayCommand]
    private async Task Start()
    {
        IsInProgress = true;
        Progress = 0;
        try
        {
            foreach (var file in Files)
            {
                var inputFile = new MediaFile { Filename = file };
                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                }


                DataReceivedEventHandler progressHandler = delegate(object _, DataReceivedEventArgs args)
                {
                    CalculateProgress(args, inputFile.Metadata.Duration);
                };
                WhisperService.OnOutput += progressHandler;


                Progress = 0;

                var settings = new WhisperSettings(
                    file,
                    "E:/Youtube",
                    "en"
                );
                await WhisperService.Start(settings);

                WhisperService.OnOutput -= progressHandler;
            }
        }
        finally
        {
            IsInProgress = false;
            Progress = 0;
        }
    }

    private void CalculateProgress(DataReceivedEventArgs args, TimeSpan totalDuration)
    {
        if (args.Data is null) return;

        var lastLine = args.Data.Split('\n').Last();
        var timestampString = lastLine.Split(']').First().Split(' ').Last();
        if (timestampString.Count(':') == 1) timestampString = $"00:{timestampString}";
        var timestamp = TimeSpan.Parse(timestampString);

        Progress = timestamp / totalDuration * 100.0;
    }
}
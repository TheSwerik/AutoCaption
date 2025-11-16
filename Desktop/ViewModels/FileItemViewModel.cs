using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Services;
using MediaToolkit;
using MediaToolkit.Model;

namespace Desktop.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    public FileItemViewModel()
    {

    }
    public FileItemViewModel(string path)
    {
        Path = path;
        DoSplitting = ConfigService.Config.DoSplitting;
        OutputLocation = ConfigService.Config.OutputLocation;
        Language = ConfigService.Config.Language;
    }
    [ObservableProperty] private string _path = "";

    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isInProgress;
    [ObservableProperty] private double _progress;

    [ObservableProperty] private bool _doSplitting;
    [ObservableProperty] private string _outputLocation;
    [ObservableProperty] private string _language;

    [RelayCommand]
    private void OpenEdit()
    {
        WeakReferenceMessenger.Default.Send(new EditInputFileMessage(this));
    }

    [RelayCommand]
    private void Delete()
    {
        if (IsInProgress)
            //TODO are you sure?
            return;

        WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(Path));
    }

    public async Task Process()
    {

        var inputFile = new MediaFile { Filename = Path };
        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);
        }


        DataReceivedEventHandler progressHandler = delegate(object _, DataReceivedEventArgs args)
        {
            CalculateProgress(args, inputFile.Metadata.Duration);
        };
        WhisperService.OnOutput += progressHandler;



        var settings = new WhisperSettings(
            Path,
            "E:/Youtube",
            "en"
        );
        await WhisperService.Start(settings);

        WhisperService.OnOutput -= progressHandler;
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
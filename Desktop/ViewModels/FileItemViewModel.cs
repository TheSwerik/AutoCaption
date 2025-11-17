using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Services;
using Desktop.Views;
using MediaToolkit;
using MediaToolkit.Model;

namespace Desktop.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _doSplitting;

    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isInProgress;
    [ObservableProperty] private string _language;
    [ObservableProperty] private string _outputLocation;
    [ObservableProperty] private string _path = "";
    [ObservableProperty] private double _progress;

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

    [RelayCommand]
    private void OpenEdit()
    {
        WeakReferenceMessenger.Default.Send(new EditInputFileMessage(this));
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!IsInProgress) WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(Path));

        var mainWindow = App.Windows.First(w => w is MainWindow);
        var dialog = new ConfirmationWindow
        {
            DataContext = new ConfirmationViewModel("Are you  sure?",
                "The File is currently being processed. Deleting the File from the view, will cancel the processing. Do you want to proceed?")
        };
        App.Windows.Add(dialog);
        var response = await dialog.ShowDialog<bool?>(mainWindow);
        App.Windows.Remove(dialog);

        if (!response ?? false) return;

        WeakReferenceMessenger.Default.Send(new CancelMessage());
        WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(Path));
        WeakReferenceMessenger.Default.Send(new StartMessage(true));
    }

    public async Task Process(CancellationToken token)
    {
        if (IsCompleted || IsInProgress) return;

        IsInProgress = true;

        //TODO file splitting
        var inputFile = new MediaFile { Filename = Path };
        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);
        }

        if (inputFile.Metadata.Duration > TimeSpan.FromMinutes(20))
        {
            //TODO convert file to temp.wav file
            //TODO split temp.wav file into 20min chunks
            //TODO process each chunk (temp.chunk1.wav, tempchunkN.wav)
            //TODO combine each chunk temp1 + temp2+1xOffset + temp3+2xOffset, etc
            // https://stackoverflow.com/questions/36632511/split-audio-file-into-several-files-each-below-a-size-threshold#:~:text=Here%20is%20a%20working%20code.
        }

        DataReceivedEventHandler progressHandler = delegate(object _, DataReceivedEventArgs args)
        {
            CalculateProgress(args, inputFile.Metadata.Duration);
        };

        WhisperService.OnOutput += progressHandler;

        var settings = new WhisperSettings(
            Path,
            $"\"{OutputLocation}\"",
            Language
        );
        try
        {
            await WhisperService.Process(settings, token);
            Progress = 100.0;
            IsCompleted = true;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"Cancelled {Path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + '\n' + ex.StackTrace);
        }
        finally
        {
            WhisperService.OnOutput -= progressHandler;
            IsInProgress = false;
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
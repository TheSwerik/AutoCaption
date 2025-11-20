using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Exceptions.WhisperService;
using Desktop.Exceptions.YouTubeService;
using Desktop.Services;
using Desktop.Views;
using Desktop.Views.Modals;

namespace Desktop.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    private readonly ILogger _logger = new Logger<FileItemViewModel>();
    [ObservableProperty] private bool _doSplitting;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isInProgress;
    [ObservableProperty] private string _language;
    [ObservableProperty] private string _outputLocation;
    [ObservableProperty] private string _path = "";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _captionUploaded;

    public FileItemViewModel()
    {
        _language = "";
        _outputLocation = "";
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
        if (!IsInProgress)
        {
            WeakReferenceMessenger.Default.Send(new RemoveInputFileMessage(Path));
            return;
        }

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

        try
        {
            IsInProgress = true;
            var settings = new WhisperSettings(Path, $"\"{OutputLocation}\"", Language, DoSplitting);
            WhisperService.OnProgress += OnProgress;
            await WhisperService.Process(settings, token);
            Progress = 100.0;
            IsCompleted = true;
            CaptionUploaded = true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation($"Cancelled {Path}");
            Progress = 0;
        }
        catch (YtdlpException e)
        {
            _logger.LogError($"yt-dlp had an error for {Path}: {e.Message}");
            var errorWindow = new ErrorWindow { DataContext = new ErrorViewModel($"Could not download the video from YouTube:\n{e.Message}") };
            await App.OpenModal<MainWindow, bool?>(errorWindow);
            Progress = 0;
        }
        catch (FfmpegException e)
        {
            _logger.LogError($"ffmpeg had an error for {Path}: {e.Message}");
            var errorWindow = new ErrorWindow { DataContext = new ErrorViewModel($"Could not split the file into segments:\n{e.Message}") };
            await App.OpenModal<MainWindow, bool?>(errorWindow);
            Progress = 0;
        }
        catch (WhisperException e)
        {
            _logger.LogError($"whisper had an error for {Path}: {e.Message}");
            var errorWindow = new ErrorWindow { DataContext = new ErrorViewModel($"Could not generate captions:\n{e.Message}") };
            await App.OpenModal<MainWindow, bool?>(errorWindow);
            Progress = 0;
        }
        catch (QuotaExceededException<string> e)
        {
            _logger.LogError($"Upload Quota exceeded at {Path}");
            if (!ConfigService.Config.GenerateWithoutUploading)
            {
                var errorWindow = new ErrorWindow
                {
                    DataContext = new ErrorViewModel(
                        $"YouTube API Quota exceeded. Could not upload the Caption to YouTube.\nThe generated Caption-File can be found at\n{e.PartialValue}")
                };
                await App.OpenModal<MainWindow, bool?>(errorWindow);
            }

            CaptionUploaded = false;
            IsCompleted = true;
            Progress = 100.0;
        }
        finally
        {
            WhisperService.OnProgress -= OnProgress;
            IsInProgress = false;
        }
    }

    private void OnProgress(object _, ProgressEventArgs args)
    {
        Progress = args.Value;
    }

    [RelayCommand]
    private void Reveal()
    {
        if (YoutubeService.IsYoutubePath(Path))
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = YoutubeService.GetYouTubeVideoUrl(YoutubeService.GetYouTubeVideoId(Path)!),
                UseShellExecute = true
            });
        }
        else
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{Path}\"",
                UseShellExecute = true
            });
        }
    }
}
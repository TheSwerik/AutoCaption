using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Desktop.Exceptions.YouTubeService;
using Desktop.Services;
using Desktop.Views;
using Desktop.Views.Modals;

namespace Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
#if DEBUG
    private static readonly string SessionPath = Path.Combine("./AutoCaption", "session.json");
#else
    private static readonly string SessionPath =
 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoCaption","session.json");
#endif

    private static readonly JsonSerializerOptions JsonOption = new()
        { WriteIndented = true, IgnoreReadOnlyFields = true, IgnoreReadOnlyProperties = true };

    private CancellationTokenSource _cancellationTokenSource = new();
    [ObservableProperty] private ObservableCollection<ViewModelBase> _fileItems = [new AddViewModel()];
    [ObservableProperty] private ObservableCollection<FileItemViewModel> _files = [];
    [ObservableProperty] private string _filesString = "";
    [ObservableProperty] private bool _isInProgress;

    public MainWindowViewModel()
    {
        if (Design.IsDesignMode) return;

        LoadSession();

        WeakReferenceMessenger.Default.Register<MainWindowViewModel, AddInputFilesMessage>(this,
            static async void (vm, _) =>
            {
                var mainWindow = App.Windows.First(w => w is MainWindow);
                var topLevel = TopLevel.GetTopLevel(mainWindow) ?? throw new UnreachableException();

                var inputOptions = new FilePickerOpenOptions
                {
                    AllowMultiple = true,
                    Title = "Input Files"
                };
                var input = await topLevel.StorageProvider.OpenFilePickerAsync(inputOptions);
                var files = input.Select(f => f.Path.LocalPath);
                vm.AddFiles(files);
            });

        WeakReferenceMessenger.Default.Register<MainWindowViewModel, RemoveInputFileMessage>(this,
            static void (vm, m) => vm.RemoveFile(m.Path));

        WeakReferenceMessenger.Default.Register<MainWindowViewModel, EditInputFileMessage>(this,
            static async void (vm, m) =>
            {
                var mainWindow = App.Windows.First(w => w is MainWindow);
                var dialog = new FileItemSettingsWindow { DataContext = new FileItemSettingsViewModel(m.File) };
                App.Windows.Add(dialog);
                var response = await dialog.ShowDialog<bool?>(mainWindow);
                App.Windows.Remove(dialog);
                vm.SaveSession();
            });

        WeakReferenceMessenger.Default.Register<MainWindowViewModel, CancelMessage>(this,
            async void (vm, _) => await vm.Cancel());
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, StartMessage>(this,
            async void (vm, m) =>
            {
                while (m.WaitUntilReady && IsInProgress) await Task.Delay(50);
                await vm.Start();
            });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    [RelayCommand]
    private async Task OpenSettingsWindow()
    {
        if (IsInProgress) return;

        var mainWindow = App.Windows.First(w => w is MainWindow);
        var dialog = new SettingsWindow { DataContext = new SettingsViewModel() };
        App.Windows.Add(dialog);
        var response = await dialog.ShowDialog<bool?>(mainWindow);
        App.Windows.Remove(dialog);
    }

    [RelayCommand]
    private async Task OpenImportFromYoutube()
    {
        var dialog = new ImportYoutubeWindow { DataContext = new ImportYoutubeViewModel() };
        var response = await App.OpenModal<MainWindow, ImportYoutubeViewModel.Result?>(dialog);

        if (response is null) return;

        ImmutableArray<FileItemViewModel> videoFiles;
        try
        {
            videoFiles = await YoutubeService.GetAllVideosWithoutCustomCaptions(response.Visibilities, response.Skip);
        }
        catch (QuotaExceededException<ImmutableArray<FileItemViewModel>?> e)
        {
            if (e.PartialValue is null)
            {
                var errorWindow = new ErrorWindow { DataContext = new ErrorViewModel("The daily Quota for YouTube has been exceeded.") };
                await App.OpenModal<MainWindow, bool?>(errorWindow);
                return;
            }

            var confirmation = new ConfirmationWindow
            {
                DataContext = new ConfirmationViewModel("Quota exceeded",
                    $"The daily Quota for YouTube has been exceeded. You can add all {e.PartialValue.Value.Length} Videos to the session and continue next time (use the skip setting) or you can abort the operation and start over tomorrow.\nDo you want to add all {e.PartialValue.Value.Length} Videos to the session?")
            };
            var confirmationResponse = await App.OpenModal<MainWindow, bool?>(confirmation);
            if (confirmationResponse is not true) return;

            videoFiles = e.PartialValue.Value;
        }
        catch (NoVisibilitySelectedException)
        {
            var errorDialog = new ErrorWindow { DataContext = new ErrorViewModel("You have not selected any Visbility, so no Videos are found.") };
            await App.OpenModal<MainWindow, bool?>(errorDialog);
            return;
        }
        catch (AuthorizationException e)
        {
            var authErrorDialog = new ErrorWindow { DataContext = new ErrorViewModel($"There was an Authorization Error:\n{e.Message}") };
            await App.OpenModal<MainWindow, bool?>(authErrorDialog);
            return;
        }
        catch (YouTubeServiceException e)
        {
            var genericErrorDialog = new ErrorWindow { DataContext = new ErrorViewModel($"There was an Error with YouTube:\n{e.Message}") };
            await App.OpenModal<MainWindow, bool?>(genericErrorDialog);
            return;
        }

        foreach (var file in videoFiles) Files.Add(file);
        RegenerateFileViews();
        SaveSession();
    }

    [RelayCommand]
    private async Task Start()
    {
        if (IsInProgress) return;

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        IsInProgress = true;
        try
        {
            for (var i = 0; i <= Files.Count; i++)
            {
                if (i >= Files.Count) break;
                if (_cancellationTokenSource.IsCancellationRequested) break;
                await Files[i].Process(_cancellationTokenSource.Token);
                SaveSession();
            }
        }
        finally
        {
            IsInProgress = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _cancellationTokenSource.CancelAsync();
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var files = Files.Where(f => f.IsCompleted).Select(f => f.Path).ToList();
        foreach (var file in files) RemoveFile(file);
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        var mainWindow = App.Windows.First(w => w is MainWindow);
        var dialog = new ConfirmationWindow
        {
            DataContext = new ConfirmationViewModel("Clear All",
                "Do you want to remove every File? Even uncompleted and currently in progress?")
        };
        App.Windows.Add(dialog);
        var response = await dialog.ShowDialog<bool?>(mainWindow);
        App.Windows.Remove(dialog);
        if (response is null || !response.Value) return;

        await Cancel();
        var files = Files.Where(f => f.IsCompleted).Select(f => f.Path).ToList();
        foreach (var file in files) RemoveFile(file);
    }

    private void AddFiles(IEnumerable<string> files)
    {
        var newFiles = files.Select(p => new FileItemViewModel(p));
        foreach (var file in newFiles) Files.Add(file);

        RegenerateFileViews();
        SaveSession();
    }

    private void RemoveFile(string path)
    {
        var file = Files.FirstOrDefault(f => f.Path.Equals(path));
        if (file is null) return;
        Files.Remove(file);
        RegenerateFileViews();
        SaveSession();
    }

    private void RegenerateFileViews()
    {
        var itemsWithAdd = Files.Cast<ViewModelBase>().Concat([new AddViewModel()]);
        FileItems = new ObservableCollection<ViewModelBase>(itemsWithAdd);

        FilesString = string.Join('\n', Files);
    }

    private void LoadSession()
    {
        if (!File.Exists(SessionPath)) return;

        var json = File.ReadAllText(SessionPath);
        var result = JsonSerializer.Deserialize<List<FileItemViewModel>>(json, JsonOption);
        if (result is null) return;

        Files = new ObservableCollection<FileItemViewModel>(result);
        RegenerateFileViews();
    }

    private void SaveSession()
    {
        var json = JsonSerializer.Serialize(Files.ToList(), JsonOption);
        File.WriteAllText(SessionPath, json);
    }
}
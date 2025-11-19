using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Desktop.Services;
using Desktop.Views.Modals;

namespace Desktop.ViewModels;

public partial class ImportYoutubeViewModel : ViewModelBase
{
    [ObservableProperty] private YouTubeVisibility[] _allVisibilities = Enum.GetValues<YouTubeVisibility>();
    [ObservableProperty] private int _skip;
    [ObservableProperty] private ObservableCollection<YouTubeVisibility> _visibilities = new([]);

    [RelayCommand]
    private void Yes()
    {
        var window = App.Windows.First(w => w is ConfirmationWindow);
        window.Close(new Result(Visibilities.ToArray(), Skip));
    }

    [RelayCommand]
    private void No()
    {
        Cancel();
    }

    private static void Cancel()
    {
        var window = App.Windows.First(w => w is ConfirmationWindow);
        window.Close();
    }

    public record Result(YouTubeVisibility[] Visibilities, int Skip);
}
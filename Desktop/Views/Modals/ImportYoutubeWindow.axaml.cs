using Avalonia.Controls;

namespace Desktop.Views.Modals;

public partial class ImportYoutubeWindow : Window
{
    public ImportYoutubeWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
using Avalonia.Controls;

namespace Desktop.Views.Modals;

public partial class FileItemSettingsWindow : Window
{
    public FileItemSettingsWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
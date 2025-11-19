using Avalonia.Controls;

namespace Desktop.Views.Modals;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
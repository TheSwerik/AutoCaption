using Avalonia.Controls;

namespace Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
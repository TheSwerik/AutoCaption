using Avalonia.Controls;

namespace Desktop.Views.Modals;

public partial class ErrorWindow : Window
{
    public ErrorWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
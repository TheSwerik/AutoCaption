using Avalonia.Controls;

namespace Desktop.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
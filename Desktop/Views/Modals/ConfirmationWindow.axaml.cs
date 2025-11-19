using Avalonia.Controls;

namespace Desktop.Views.Modals;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
    }
}
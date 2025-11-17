using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace Desktop.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;

        WeakReferenceMessenger.Default.Register<ConfirmationWindow, CloseConfirmationWindowMessage>(this,
            static void (w, m) => w.Close(m.Result));
    }
}
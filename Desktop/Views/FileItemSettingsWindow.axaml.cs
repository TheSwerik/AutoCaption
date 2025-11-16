using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace Desktop.Views;

public partial class FileItemSettingsWindow : Window
{
    public FileItemSettingsWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;

        WeakReferenceMessenger.Default.Register<FileItemSettingsWindow, CloseFileSettingsMessage>(this,
            static void (w, m) => w.Close(m.Result));
    }
}
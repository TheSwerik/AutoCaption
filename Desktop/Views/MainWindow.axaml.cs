using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        var opt = new FilePickerOpenOptions();

        var file = await StorageProvider.OpenFilePickerAsync(opt);

        List<string> arguments =
        [
            "whisper",
            $"\"{file[0].Path.LocalPath}\"",
            "--device cuda",
            "-o E:/YouTube/captions/",
            "--output_format all",
            "--model medium",
            "--language English"
        ];
        var command =
            @"whisper 'D:\Old Videos\YGO Beginning 3 2.0.mov' --device cuda -o E:/YouTube/captions/ --output_format all --model medium --language English";

        var x = file[0].Path.LocalPath;
        Console.WriteLine(x);

        var info = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = "python3",
            Arguments = $"-m {string.Join(' ', arguments)}",
            UseShellExecute = false
        };
        // var proc = Process.Start($"python3 -m {command}");
        var proc = Process.Start(info);
        proc.OutputDataReceived += (a, b) => Console.WriteLine(b.Data);
        proc.ErrorDataReceived += (a, b) => Console.WriteLine(b.Data);
        proc.WaitForExit(TimeSpan.FromMinutes(10));

        Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
        Console.WriteLine(await proc.StandardError.ReadToEndAsync());

        if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);
    }
}
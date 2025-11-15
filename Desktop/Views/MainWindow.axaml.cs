using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Desktop.Services;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        #region Variables, that should be selected per file

        var language = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
        var outputLocation = "\"E:/YouTube/captions/\"";

        #endregion

        var opt = new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = "Input Files"
        };
        var files = await StorageProvider.OpenFilePickerAsync(opt);

        foreach (var file in files)
        {
            List<string> arguments =
            [
                "whisper",
                $"\"{file.Path.LocalPath}\"",
                $"--device {(ConfigService.Config.UseGpu ? "cuda" : "cpu")}",
                $"-o {outputLocation}",
                $"--output_format {ConfigService.Config.OutputFormat.ToString().ToLowerInvariant()}",
                $"--model {ConfigService.Config.Model.ToString().ToLowerInvariant()}",
                $"--language {language.TwoLetterISOLanguageName}"
            ];

            var info = new ProcessStartInfo
            {
                FileName = ConfigService.Config.PythonLocation,
                Arguments = $"-m {string.Join(' ', arguments)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Console.WriteLine(info.Arguments);
            var proc = new Process();
            proc.StartInfo = info;
            Console.WriteLine(1);

            proc.OutputDataReceived += (a, b) => Console.WriteLine(a + " " + b.Data);
            proc.ErrorDataReceived += (a, b) => Console.WriteLine("ERRRORF " + a + " " + b.Data);

            Console.WriteLine(1.5);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            Console.WriteLine(2);

            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        #region Variables, that should be configurable

        var python = @"python3";
        var useGpu = true;
        // var outPutFormat = "all";
        var outPutFormat = "vtt";
        var model = "medium";

        #endregion

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
                $"--device {(useGpu ? "cuda" : "cpu")}",
                $"-o {outputLocation}",
                $"--output_format {outPutFormat}",
                $"--model {model}",
                $"--language {language.TwoLetterISOLanguageName}"
            ];

            var info = new ProcessStartInfo
            {
                FileName = python,
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


            // var i = 0;
            // while (!proc.HasExited)
            // {
            //     // Console.WriteLine(3);
            //     // var asd = await proc.StandardOutput.ReadToEndAsync();
            //     // Console.WriteLine(4);
            //     // var asd2 = await proc.StandardError.ReadToEndAsync();
            //     // Console.WriteLine(5);
            //     // Console.WriteLine(asd);
            //     // Console.WriteLine(asd2);
            //     Console.WriteLine("READ LINE");
            //     proc.BeginOutputReadLine();
            //     Console.WriteLine(i++);
            //     await Task.Delay(1000);
            // }

            Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
            Console.WriteLine(await proc.StandardError.ReadToEndAsync());

            if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);
        }
    }
}
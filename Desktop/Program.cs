using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Desktop.Services;

namespace Desktop;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        InstallWhisper(true);
#endif
        ConfigService.Init(args);
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }


    private static void InstallWhisper(bool update)
    {
        //TODO move to Github Actions
        List<string> arguments = ["whisper", "-h"];

        var info = new ProcessStartInfo
        {
            FileName = ConfigService.Config.PythonLocation,
            Arguments = $"-m {string.Join(' ', arguments)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.Environment["PATH"] = $"{info.Environment["PATH"]};./tools/ffmpeg/bin";
        info.Environment["PYTHONUTF8"] = "1";

        using var process = new Process();
        process.StartInfo = info;

        process.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
        process.ErrorDataReceived += (_, args) => Console.WriteLine("ERRROROR: " + args.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode == 0) return;

        Console.WriteLine("installing whisper");

        arguments = ["pip", "install", "--upgrade", "openai-whisper"];

        info = new ProcessStartInfo
        {
            FileName = ConfigService.Config.PythonLocation,
            Arguments = $"-m {string.Join(' ', arguments)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.Environment["PATH"] = $"{info.Environment["PATH"]};./tools/ffmpeg/bin";
        info.Environment["PYTHONUTF8"] = "1";

        using var process2 = new Process();
        process2.StartInfo = info;

        process2.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
        process2.ErrorDataReceived += (_, args) => Console.WriteLine("ERRROROR: " + args.Data);
        process2.Start();
        process2.BeginOutputReadLine();
        process2.BeginErrorReadLine();
        process2.WaitForExit();

        arguments =
        [
            "pip", "install", "--upgrade", "torch", "torchvision", "torchaudio",
            "--index-url https://download.pytorch.org/whl/cu121"
        ];

        info = new ProcessStartInfo
        {
            FileName = ConfigService.Config.PythonLocation,
            Arguments = $"-m {string.Join(' ', arguments)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.Environment["PATH"] = $"{info.Environment["PATH"]};./tools/ffmpeg/bin";
        info.Environment["PYTHONUTF8"] = "1";

        using var process3 = new Process();
        process3.StartInfo = info;

        process3.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
        process3.ErrorDataReceived += (_, args) => Console.WriteLine("ERRROROR: " + args.Data);
        process3.Start();
        process3.BeginOutputReadLine();
        process3.BeginErrorReadLine();
        process3.WaitForExit();
    }
}
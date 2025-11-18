using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
        InstallWhisper(false);
        InstallFFmpeg(false);
        ConfigService.Init(args);
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static void InstallFFmpeg(bool update)
    {
        //TODO move to Github Actions
        const string downloadUrl =
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        const string ffmpegFolder = "./tools/ffmpeg";
        const string ffmpegPath = $"{ffmpegFolder}/bin/ffmpeg.exe";
        if (!update && File.Exists(ffmpegPath))
        {
            Console.WriteLine("ffmpeg found");
            return;
        }

        Console.WriteLine("installing ffmpeg");

        using var client = new HttpClient();
        using var response = client.GetStreamAsync(downloadUrl).Result;
        ZipFile.ExtractToDirectory(response, "./tools");

        const string tempDir = "./tools/ffmpeg-master-latest-win64-gpl";
        if (Directory.Exists(ffmpegFolder)) Directory.Delete(ffmpegFolder, true);
        Directory.Move(tempDir, ffmpegFolder);
        // Directory.Delete(tempDir,true);

        Console.WriteLine("ffmpeg insalled");
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
    }
}
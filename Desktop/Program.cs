using System;
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
}
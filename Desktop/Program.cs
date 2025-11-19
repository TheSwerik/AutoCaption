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
    private static readonly ILogger Logger = new Logger<Program>();
    private static readonly ILogger WhisperLogger = new Logger("Whisper");

    private static readonly ILogger PipLogger = new Logger("Pip");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        InstallWhisper(false);
        InstallFFmpeg(false);
        InstallYtDlp(false);
#endif
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

#if DEBUG
    private static void InstallFFmpeg(bool update)
    {
        const string downloadUrl =
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        const string ffmpegFolder = "./tools/ffmpeg";
        const string ffmpegPath = $"{ffmpegFolder}/bin/ffmpeg.exe";
        if (!update && File.Exists(ffmpegPath))
        {
            Logger.LogInformation("ffmpeg found");
            return;
        }

        Logger.LogInformation("installing ffmpeg");

        using var client = new HttpClient();
        using var response = client.GetStreamAsync(downloadUrl).Result;
        ZipFile.ExtractToDirectory(response, "./tools");

        const string tempDir = "./tools/ffmpeg-master-latest-win64-gpl";
        if (Directory.Exists(ffmpegFolder)) Directory.Delete(ffmpegFolder, true);
        Directory.Move(tempDir, ffmpegFolder);

        Logger.LogInformation("ffmpeg installed");
    }

    private static void InstallYtDlp(bool update)
    {
        const string downloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_win.zip";
        const string ytdlpFolder = "./tools/yt-dlp";
        const string ytdlpPath = $"{ytdlpFolder}/yt-dlp.exe";
        if (!update && File.Exists(ytdlpPath))
        {
            Logger.LogInformation("yt-dlp found");
            return;
        }

        Logger.LogInformation("installing yt-dlp");

        using var client = new HttpClient();
        using var response = client.GetStreamAsync(downloadUrl).Result;
        ZipFile.ExtractToDirectory(response, ytdlpFolder);

        Logger.LogInformation("yt-dlp installed");
    }

    private static void InstallWhisper(bool update)
    {
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

        process.OutputDataReceived += LogWhisperOutput;
        process.ErrorDataReceived += LogWhisperError;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.OutputDataReceived -= LogWhisperOutput;
        process.ErrorDataReceived -= LogWhisperError;

        if (process.ExitCode == 0) return;

        Logger.LogInformation("installing whisper");

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

        process2.OutputDataReceived += LogPipOutput;
        process2.ErrorDataReceived += LogPipError;
        process2.Start();
        process2.BeginOutputReadLine();
        process2.BeginErrorReadLine();
        process2.WaitForExit();
        process2.OutputDataReceived -= LogPipOutput;
        process2.ErrorDataReceived -= LogPipError;

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

        process2.OutputDataReceived += LogPipOutput;
        process2.ErrorDataReceived += LogPipError;
        process3.Start();
        process3.BeginOutputReadLine();
        process3.BeginErrorReadLine();
        process3.WaitForExit();
        process2.OutputDataReceived -= LogPipOutput;
        process2.ErrorDataReceived -= LogPipError;
    }

    private static void LogWhisperOutput(object? sender, DataReceivedEventArgs args)
    {
        WhisperLogger.LogDebug(args.Data);
    }

    private static void LogWhisperError(object? sender, DataReceivedEventArgs args)
    {
        WhisperLogger.LogError(args.Data);
    }

    private static void LogPipOutput(object? sender, DataReceivedEventArgs args)
    {
        PipLogger.LogDebug(args.Data);
    }

    private static void LogPipError(object? sender, DataReceivedEventArgs args)
    {
        PipLogger.LogError(args.Data);
    }
#endif
}
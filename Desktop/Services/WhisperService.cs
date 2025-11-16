using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Desktop.Services;

public static class WhisperService
{
    public static event DataReceivedEventHandler? OnOutput;
    public static event DataReceivedEventHandler? OnError;

    public static async Task Process(params WhisperSettings[] settings)
    {
        foreach (var setting in settings) await Process(setting);
    }

    public static async Task Process(WhisperSettings settings, CancellationToken ct)
    {
        List<string> arguments =
        [
            "whisper",
            $"\"{settings.FilePath}\"",
            $"--device {(ConfigService.Config.UseGpu ? "cuda" : "cpu")}",
            $"-o {settings.OutputLocation}",
            $"--output_format {ConfigService.Config.OutputFormat.ToString().ToLowerInvariant()}",
            $"--model {ConfigService.Config.Model.ToString().ToLowerInvariant()}",
            $"--language {settings.Language}"
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

        proc.OutputDataReceived += OutputReceived;
        proc.ErrorDataReceived += ErrorReceived;

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);
    }

    private static void OutputReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(sender + " " + e.Data);
        OnOutput?.Invoke(sender, e);
    }

    private static void ErrorReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine("ERROR: " + sender + " " + e.Data);
        OnOutput?.Invoke(sender, e);
    }
}

public record WhisperSettings(string FilePath, string OutputLocation, string Language);
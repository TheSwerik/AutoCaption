using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;

namespace Desktop.Services;

public static class WhisperService
{
    public static event ProgressEventHandler? OnProgress;

    public static async Task Process(WhisperSettings settings, CancellationToken ct)
    {
        var inputFile = new MediaFile { Filename = settings.FilePath };
        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);
        }

        //file splitting
        if (inputFile.Metadata.Duration > TimeSpan.FromMinutes(30))
        {
            //TODO convert file to temp.wav file
            //TODO split temp.wav file into 20min chunks
            //TODO process each chunk (temp.chunk1.wav, tempchunkN.wav)
            //TODO combine each chunk temp1 + temp2+1xOffset + temp3+2xOffset, etc
            // https://stackoverflow.com/questions/36632511/split-audio-file-into-several-files-each-below-a-size-threshold#:~:text=Here%20is%20a%20working%20code.
        }

        await Process(settings, inputFile.Metadata.Duration, ct);
    }

    private static async Task Process(WhisperSettings settings, TimeSpan totalDuration, CancellationToken ct)
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
        using var proc = new Process();
        proc.StartInfo = info;

        proc.OutputDataReceived += ProgressHandler;
        proc.OutputDataReceived += OutputReceived;
        proc.ErrorDataReceived += ErrorReceived;

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(ct);
        if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);

        proc.OutputDataReceived -= ProgressHandler;
        proc.OutputDataReceived -= OutputReceived;
        proc.ErrorDataReceived -= ErrorReceived;
        return;

        void ProgressHandler(object _, DataReceivedEventArgs args)
        {
            CalculateProgress(args, totalDuration);
        }
    }

    private static void OutputReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(sender + " " + e.Data);
    }

    private static void CalculateProgress(DataReceivedEventArgs args, TimeSpan totalDuration)
    {
        if (args.Data is null) return;

        var lastLine = args.Data.Split('\n').Last();
        var timestampString = lastLine.Split(']').First().Split(' ').Last();
        if (timestampString.Count(':') == 1) timestampString = $"00:{timestampString}";
        var timestamp = TimeSpan.Parse(timestampString);

        var progress = timestamp / totalDuration * 100.0;
        OnProgress?.Invoke(null, new ProgressEventArgs(progress));
    }

    private static void ErrorReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine("ERROR: " + sender + " " + e.Data);
    }
}

public record WhisperSettings(string FilePath, string OutputLocation, string Language);

public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

public class ProgressEventArgs(double value) : EventArgs
{
    public readonly double Value = value;
}
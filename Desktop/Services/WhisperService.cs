using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;

namespace Desktop.Services;

public static partial class WhisperService
{
    public static event ProgressEventHandler? OnProgress;

    public static async Task Process(WhisperSettings settings, CancellationToken ct)
    {
        var maxDuration = ConfigService.Config.ChunkSize;

        var inputFile = new MediaFile { Filename = settings.FilePath };
        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);
        }

        if (inputFile.Metadata.Duration < maxDuration)
        {
            await Process(settings, inputFile.Metadata.Duration, ct);
            return;
        }

        var tempPath = $"{settings.OutputLocation.Replace("\"", "")}/temp";
        try
        {
            // split file into 30min segments
            await SplitFile(settings.FilePath, tempPath, maxDuration, ct);
            var ext = Path.GetExtension(settings.FilePath);

            // process each segment
            var segments = (int)Math.Ceiling(inputFile.Metadata.Duration / maxDuration);
            for (var i = 0; i < segments; i++)
            {
                var segmentSettings = new WhisperSettings(
                    $"\"{tempPath}/segment-{i}{ext}\"",
                    $"\"{tempPath}/\"",
                    settings.Language
                );
                await Process(segmentSettings, inputFile.Metadata.Duration, ct);
            }

            // recombine segments
            if (ConfigService.Config.OutputFormat == OutputFormat.ALL)
            {
                //TODO yikes
            }
            else
            {
                var captionExtension = ConfigService.Config.OutputFormat.ToString().ToLowerInvariant();
                var segmentFiles = Enumerable.Range(0, segments)
                    .Select(i => $"\"{tempPath}/segment-{i}.{captionExtension}\"");
                var outputFile =
                    $"{settings.OutputLocation}/{Path.GetFileNameWithoutExtension(settings.FilePath)}.{captionExtension}";
                await Combine(maxDuration, outputFile, segmentFiles);
            }
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
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

        Console.WriteLine(info.FileName + " " + info.Arguments);
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
        //TODO log properly
        Console.WriteLine(sender + " " + e.Data);
    }

    private static void CalculateProgress(DataReceivedEventArgs args, TimeSpan totalDuration)
    {
        if (args.Data is null) return;

        //TODO fix for chunked data
        var lastLine = args.Data.Split('\n').Last();
        var timestampString = lastLine.Split(']').First().Split(' ').Last();
        if (timestampString.Count(':') < 1) return;
        if (timestampString.Count(':') == 1) timestampString = $"00:{timestampString}";
        var timestamp = TimeSpan.Parse(timestampString);

        var progress = timestamp / totalDuration * 100.0;
        OnProgress?.Invoke(null, new ProgressEventArgs(progress));
    }

    private static void ErrorReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine("ERROR: " + sender + " " + e.Data);
    }

    private static async Task SplitFile(string path, string tempPath, TimeSpan maxTime, CancellationToken ct)
    {
        Directory.CreateDirectory(tempPath);

        var ext = Path.GetExtension(path);
        var outputTemplate = Path.Combine(tempPath, $"segment-%d{ext}");

        string[] arguments =
        [
            $"-i \"{path}\"",
            await IsAudio(path, ct) ? "" : "-vn -acodec copy",
            "-f segment",
            $"-segment_time {maxTime.TotalSeconds}",
            "-reset_timestamps 1",
            $"\"{outputTemplate}\""
        ];

        var startInfo = new ProcessStartInfo
        {
            FileName = $"{ConfigService.Config.FfmpegLocation}/ffmpeg.exe",
            Arguments = string.Join(' ', arguments),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = new Process();
        proc.StartInfo = startInfo;
        proc.EnableRaisingEvents = true;

        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERROR: " + e.Data);
        };

        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("OUTPUT: " + e.Data);
        };

        proc.Start();
        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();

        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0) throw new Exception("FFmpeg Exitcode: " + proc.ExitCode);

        Console.WriteLine("ffmpeg");
        Console.WriteLine(string.Join('\n', Directory.GetFiles(tempPath)));
    }

    private static async Task<bool> IsAudio(string path, CancellationToken ct)
    {
        var info = new ProcessStartInfo
        {
            FileName = $"{ConfigService.Config.FfmpegLocation}/ffprobe.exe",
            Arguments = $"-v error -select_streams v -show_entries stream=codec_type -of csv=p=0 \"{path}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process();
        p.StartInfo = info;
        p.Start();
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);

        return string.IsNullOrWhiteSpace(output);
    }

    private static async Task Combine(TimeSpan segemntDuration, string output, params IEnumerable<string> input)
    {
        var timeStampRegex = ConfigService.Config.OutputFormat switch
        {
            OutputFormat.VTT => TimestampRegex(),
            OutputFormat.JSON => TimestampRegex(), //TODO
            OutputFormat.TXT => TimestampRegex(), //TODO
            OutputFormat.SRT => TimestampRegex(), //TODO
            OutputFormat.TSV => TimestampRegex(), //TODO
            OutputFormat.ALL => throw new UnreachableException(),
            _ => throw new UnreachableException()
        };

        var skip = false;
        var lines = new List<string>();
        var i = 0;
        foreach (var file in input)
        {
            foreach (var line in await File.ReadAllLinesAsync(file.Replace("\"", "")))
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                if (line == "WEBVTT")
                {
                    skip = true;
                    continue;
                }

                if (!timeStampRegex.IsMatch(line))
                {
                    lines.Add(line);
                    continue;
                }

                //TODO for other fileTypes
                var timestamps = line.Split(" --> ")
                    .Select(t => t.Count(c => c == ':') > 1 ? t : $"00:{t}")
                    .Select(TimeSpan.Parse)
                    .Select(t => t + i * segemntDuration)
                    .Select(t => t.ToString("G"));
                lines.Add(string.Join(" --> ", timestamps));
            }

            i++;
        }

        await File.WriteAllLinesAsync(output.Replace("\"", ""), lines);
        Console.WriteLine(output);
    }

    [GeneratedRegex(@"(\d\d:)+(\d\d.\d+) --> (\d\d:)+(\d\d.\d+)")]
    private static partial Regex TimestampRegex();
}

public record WhisperSettings(string FilePath, string OutputLocation, string Language);

public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

public class ProgressEventArgs(double value) : EventArgs
{
    public readonly double Value = value;
}
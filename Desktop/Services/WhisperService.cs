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
    private static readonly ILogger Logger = new Logger(nameof(WhisperService));
    private static readonly ILogger WhisperLogger = new Logger("Whisper");
    private static readonly ILogger FfmpegLogger = new Logger("FFmpeg");
    public static event ProgressEventHandler? OnProgress;

    public static async Task Process(WhisperSettings settings, CancellationToken ct)
    {
        var tempPath = $"{settings.OutputLocation.Replace("\"", "")}/temp";
        var isYoutube = YoutubeService.IsYoutubePath(settings.FilePath);
        var youtubeVideoId = YoutubeService.GetYouTubeVideoId(settings.FilePath);

        if (isYoutube)
        {
            var path = await YoutubeService.DownloadAudioAsync(settings.FilePath, tempPath, ct);
            settings = settings with { FilePath = path };
        }

        var maxDuration = ConfigService.Config.ChunkSize;

        var inputFile = new MediaFile { Filename = settings.FilePath };
        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);
        }

        if (inputFile.Metadata.Duration < maxDuration)
        {
            await Process(settings, inputFile.Metadata.Duration, TimeSpan.Zero, ct);
            if (!isYoutube) return;

            var captionExtension = ConfigService.Config.OutputFormat.ToString().ToLowerInvariant();
            var vttOutputFile = $"{settings.OutputLocation}/{Path.GetFileNameWithoutExtension(settings.FilePath)}.{captionExtension}";
            await YoutubeService.UploadCaptionAsync(youtubeVideoId!, settings.Language, vttOutputFile, ct);
            return;
        }

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
                await Process(segmentSettings, inputFile.Metadata.Duration, i * maxDuration, ct);
            }

            // recombine segments
            switch (ConfigService.Config.OutputFormat)
            {
                case OutputFormat.ALL:
                {
                    OutputFormat[] extensions = [OutputFormat.SRT, OutputFormat.VTT, OutputFormat.TSV];
                    foreach (var extension in extensions)
                    {
                        var captionExtension = extension.ToString().ToLowerInvariant();
                        var segmentFiles = Enumerable.Range(0, segments)
                            .Select(i => $"\"{tempPath}/segment-{i}.{captionExtension}\"");
                        var outputFile =
                            $"{settings.OutputLocation}/{Path.GetFileNameWithoutExtension(settings.FilePath)}.{captionExtension}";
                        await Combine(maxDuration, outputFile, extension, segmentFiles);
                    }

                    break;
                }
                case OutputFormat.JSON:
                case OutputFormat.TXT:
                    Logger.LogError($"{ConfigService.Config.OutputFormat} Recombination is not supported");
                    break;
                case OutputFormat.VTT:
                case OutputFormat.SRT:
                case OutputFormat.TSV:
                default:
                {
                    var captionExtension = ConfigService.Config.OutputFormat.ToString().ToLowerInvariant();
                    var segmentFiles = Enumerable.Range(0, segments)
                        .Select(i => $"\"{tempPath}/segment-{i}.{captionExtension}\"");
                    var outputFile = $"{settings.OutputLocation}/{Path.GetFileNameWithoutExtension(settings.FilePath)}.{captionExtension}";
                    await Combine(maxDuration, outputFile, ConfigService.Config.OutputFormat, segmentFiles);
                    break;
                }
            }

            if (isYoutube)
            {
                var captionExtension = ConfigService.Config.OutputFormat.ToString().ToLowerInvariant();
                if (ConfigService.Config.OutputFormat == OutputFormat.ALL) captionExtension = "vtt";
                var vttOutputFile = $"{settings.OutputLocation}/{Path.GetFileNameWithoutExtension(settings.FilePath)}.{captionExtension}";
                await YoutubeService.UploadCaptionAsync(youtubeVideoId!, settings.Language, vttOutputFile, ct);
            }
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    private static async Task Process(WhisperSettings settings, TimeSpan totalDuration, TimeSpan segmentOffset,
        CancellationToken ct)
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

        Logger.LogInformation(info.FileName + " " + info.Arguments);
        using var proc = new Process();
        proc.StartInfo = info;

        proc.OutputDataReceived += ProgressHandler;
        proc.OutputDataReceived += WhisperInfoLogger;
        proc.ErrorDataReceived += WhisperErrorLogger;

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(ct);
        if (proc.ExitCode != 0) throw new Exception("Python Exitcode: " + proc.ExitCode);

        proc.OutputDataReceived -= ProgressHandler;
        proc.OutputDataReceived -= WhisperInfoLogger;
        proc.ErrorDataReceived -= WhisperErrorLogger;
        return;

        void ProgressHandler(object _, DataReceivedEventArgs args)
        {
            CalculateProgress(args, totalDuration, segmentOffset);
        }
    }

    private static void WhisperInfoLogger(object sender, DataReceivedEventArgs e)
    {
        WhisperLogger.LogDebug(e.Data);
    }

    private static void WhisperErrorLogger(object sender, DataReceivedEventArgs e)
    {
        WhisperLogger.LogError(e.Data);
    }

    private static void FfmpegInfoLogger(object sender, DataReceivedEventArgs e)
    {
        FfmpegLogger.LogDebug(e.Data);
    }

    private static void CalculateProgress(DataReceivedEventArgs args, TimeSpan totalDuration, TimeSpan segmentOffset)
    {
        if (args.Data is null) return;

        var lastLine = args.Data.Split('\n').Last();
        var timestampString = lastLine.Split(']').First().Split(' ').Last();
        if (timestampString.Count(':') < 1) return;
        if (timestampString.Count(':') == 1) timestampString = $"00:{timestampString}";
        var timestamp = TimeSpan.Parse(timestampString);

        var progress = (segmentOffset + timestamp) / totalDuration * 100.0;
        OnProgress?.Invoke(null, new ProgressEventArgs(progress));
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

        proc.ErrorDataReceived += FfmpegInfoLogger;
        proc.OutputDataReceived += FfmpegInfoLogger;

        proc.Start();
        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();

        await proc.WaitForExitAsync(ct);

        proc.ErrorDataReceived -= FfmpegInfoLogger;
        proc.OutputDataReceived -= FfmpegInfoLogger;

        if (proc.ExitCode != 0) throw new Exception("FFmpeg Exitcode: " + proc.ExitCode);
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

    private static async Task Combine(TimeSpan segmentDuration, string output, OutputFormat format,
        params IEnumerable<string> input)
    {
        var timeStampRegex = format switch
        {
            OutputFormat.VTT or OutputFormat.SRT => VttTimestampRegex(),
            OutputFormat.TSV => TsvTimestampRegex(),
            OutputFormat.JSON or OutputFormat.ALL => throw new UnreachableException(),
            _ => throw new UnreachableException()
        };

        var firstHeader = true;
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

                // skip headers
                if (line is "WEBVTT" or "start\tend\ttext")
                {
                    if (firstHeader)
                    {
                        firstHeader = false;
                    }
                    else
                    {
                        if (line is "WEBVTT") skip = true;
                        continue;
                    }
                }

                // add text lines
                if (!timeStampRegex.IsMatch(line))
                {
                    lines.Add(line);
                    continue;
                }

                // add timestamped lines
                if (format == OutputFormat.TSV)
                {
                    var parts = line.Split("\t");
                    var start = long.Parse(parts[0]);
                    start += i * (long)segmentDuration.TotalSeconds * 1000;
                    var end = long.Parse(parts[1]);
                    end += i * (long)segmentDuration.TotalSeconds * 1000;

                    lines.Add($"{start}\t{end}\t{parts[2]}");
                }
                else
                {
                    var timestamps = line.Split(" --> ")
                        .Select(t => t.Count(c => c == ':') > 1 ? t : $"00:{t}")
                        .Select(TimeSpan.Parse)
                        .Select(t => t + i * segmentDuration)
                        .Select(t => t.ToString("G"));
                    lines.Add(string.Join(" --> ", timestamps));
                }
            }

            i++;
        }

        await File.WriteAllLinesAsync(output.Replace("\"", ""), lines);
    }

    [GeneratedRegex(@"(\d\d:)+(\d\d.\d+) --> (\d\d:)+(\d\d.\d+)")]
    private static partial Regex VttTimestampRegex();

    [GeneratedRegex(@"\d+\t\d+\t.*")]
    private static partial Regex TsvTimestampRegex();
}

public record WhisperSettings(string FilePath, string OutputLocation, string Language);

public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);

public class ProgressEventArgs(double value) : EventArgs
{
    public readonly double Value = value;
}
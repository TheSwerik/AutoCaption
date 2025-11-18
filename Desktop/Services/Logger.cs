using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Desktop.Services;

public interface ILogger
{
    void LogInformation(string? message);
    void LogDebug(string? message);
    void LogError(string? message);
}

public class Logger(string sender) : ILogger
{
    private const string ResetColor = "\e[0m";
    private const string Info = "\e[1;32m";
    private const string Debug = "\e[1;37m";
    private const string Error = "\e[1;31m";

    private static Timer _flushTimer = null!;

    private static StreamWriter FileStream
    {
        get
        {
            if (field is not null) return field;

#if DEBUG
            const string roaming = "./";
#else
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
            var settingsLocation = Path.Combine(roaming, "AutoCaption");
            var filePath = Path.Combine(settingsLocation, "logs.txt");
            field = new StreamWriter(filePath);

            _flushTimer = new Timer(_ => FileStream.Flush(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return field;
        }
    }

    public void LogInformation(string? message)
    {
        Log(Loglevel.Info, message);
    }

    public void LogDebug(string? message)
    {
        Log(Loglevel.Debug, message);
    }

    public void LogError(string? message)
    {
        Log(Loglevel.Error, message);
    }

    private void Log(Loglevel level, string? message)
    {
        if (message is null) return;
        if (level > ConfigService.Config.LogLevel) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var color = level switch
        {
            Loglevel.Error => Error,
            Loglevel.Info => Info,
            Loglevel.Debug => Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        var levelString = $"[{level}]";

        Console.WriteLine($"{timestamp} {color}{levelString,-7}{ResetColor} {sender}: {message}");

        if (!ConfigService.Config.LogToFile) return;
        FileStream.WriteLine($"{timestamp} {levelString,-7} {sender}: {message}");
    }

    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    public static void Dispose()
    {
        FileStream?.Dispose();
        _flushTimer?.Dispose();
    }
}

public class Logger<T>() : Logger(typeof(T).Name);

public enum Loglevel
{
    Error = 0,
    Info = 1,
    Debug = 2
}
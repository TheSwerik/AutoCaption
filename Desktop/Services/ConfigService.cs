using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desktop.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions;

    static ConfigService()
    {
        JsonOptions = new JsonSerializerOptions();
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
        JsonOptions.WriteIndented = true;


        if (FromSettings())
        {
            Save();
            return;
        }

        // Default Values
        Config = new Configuration
        {
            PythonLocation = "./tools/python/python.exe",
            FfmpegLocation = "./tools/ffmpeg/bin",
            OutputFormat = OutputFormat.VTT,
            Model = Model.Turbo,
            UseGpu = true,
            ChunkSize = TimeSpan.FromMinutes(30),
            DoSplitting = true,
            Language = "en",
            OutputLocation = "./"
        };
        Save();
    }

    public static Configuration Config { get; private set; } = null!;

    public static void Init(params string[] args)
    {
        // Config.Add
    }

    private static bool FromSettings()
    {
#if DEBUG
        const string roaming = "./";
#else
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
        var settingsLocation = Path.Combine(roaming, "AutoCaption");
        var filePath = Path.Combine(settingsLocation, "config.json");
        if (Directory.Exists(settingsLocation))
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Configuration>(json, JsonOptions);
                if (config is not null)
                {
                    Config = config;
                    return true;
                }
            }

            using var _ = File.Create(filePath);
            return false;
        }

        Directory.CreateDirectory(settingsLocation);
        using var _2 = File.Create(filePath);
        return false;
    }

    private static void Save()
    {
#if DEBUG
        const string roaming = "./";
#else
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
        var settingsLocation = Path.Combine(roaming, "AutoCaption");
        var filePath = Path.Combine(settingsLocation, "config.json");

        var json = JsonSerializer.Serialize(Config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public class Configuration
    {
        public OutputFormat OutputFormat
        {
            get;
            set
            {
                field = value;
                Save();
            }
        }

        public string PythonLocation
        {
            get;
            set
            {
                field = value.Replace('\\', '/');
                Save();
            }
        } = null!;

        public string FfmpegLocation
        {
            get;
            set
            {
                field = value.Replace('\\', '/');
                Save();
            }
        } = null!;

        public bool UseGpu
        {
            get;
            set
            {
                field = value;
                Save();
            }
        }

        public Model Model
        {
            get;
            set
            {
                field = value;
                Save();
            }
        }

        public bool DoSplitting
        {
            get;
            set
            {
                field = value;
                Save();
            }
        }

        public string OutputLocation
        {
            get;
            set
            {
                field = value.Replace('\\', '/');
                Save();
            }
        } = "";

        public string Language
        {
            get;
            set
            {
                field = value;
                Save();
            }
        } = "";

        public TimeSpan ChunkSize
        {
            get;
            set
            {
                field = value;
                Save();
            }
        } = TimeSpan.FromMinutes(20);

        public Loglevel LogLevel
        {
            get;
            set
            {
                field = value;
                Save();
            }
        } = Loglevel.Info; //TODO

        public bool LogToFile
        {
            get;
            set
            {
                field = value;
                Save();
            }
        } = true; //TODO
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum OutputFormat
{
    VTT,
    JSON,
    TXT,
    SRT,
    TSV,
    ALL
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum Model
{
    Tiny,
    Base,
    Small,
    Medium,
    Large,
    Turbo
}
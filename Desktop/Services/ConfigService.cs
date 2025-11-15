using System;
using System.IO;
using System.Text.Json;

namespace Desktop.Services;

public static class ConfigService
{
    static ConfigService()
    {
        if (FromSettings())
        {
            Save();
            return;
        }

        // Default Values
        Config = new Configuration
        {
            OutputFormat = OutputFormat.VTT,
            Model = Model.Medium,
            PythonLocation = "python3",
            UseGpu = true
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
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsLocation = Path.Combine(roaming, "AutoCaption");
        var filePath = Path.Combine(settingsLocation, "config.json");
        if (Directory.Exists(settingsLocation))
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Configuration>(json);
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
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsLocation = Path.Combine(roaming, "AutoCaption");
        var filePath = Path.Combine(settingsLocation, "config.json");

        var json = JsonSerializer.Serialize(Config);
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
                field = value;
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
    }
}

public enum Config
{
    OutputFormat,
    PythonLocation,
    UseGpu,
    Model
}

public enum OutputFormat
{
    VTT,
    JSON,
    TXT,
    SRT,
    TSV,
    ALL
}

public enum Model
{
    Small,
    Medium,
    Large
}
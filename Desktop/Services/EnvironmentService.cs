using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Desktop.Services;

public static class EnvironmentService
{
    private static readonly ILogger Logger = new Logger("Environment");

    private static readonly string[] Packages = ["openai-whisper", "yt-dlp", "imageio-ffmpeg"];

    /// <summary>
    /// Ensures the virtual environment exists, has the required packages,
    /// and wires the resolved tool paths into the configuration.
    /// </summary>
    public static async Task EnsureVirtualEnvAsync()
    {
        var venvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".venv");

        if (!Directory.Exists(venvPath))
        {
            Logger.LogInformation("Creating virtual environment in .venv...");
            CreateVenv(venvPath);
        }

        var pythonExe = GetVenvExecutable(venvPath, "python");
        ConfigService.Config.PythonExecutable = pythonExe;

        await InstallDependenciesAsync(pythonExe);
        await ConfigureToolPathsAsync(venvPath, pythonExe);
    }

    private static void CreateVenv(string venvPath)
    {
        try
        {
            // Use the system python to create a venv.
            // This assumes 'python' or 'python3' is in PATH.
            var pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-m venv \"{venvPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                throw new Exception("Failed to create virtual environment. Ensure python is installed and in PATH.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating venv: {ex.Message}");
            throw;
        }
    }

    private static async Task InstallDependenciesAsync(string pythonExe)
    {
        foreach (var package in Packages)
        {
            if (await IsInstalledAsync(pythonExe, package))
            {
                Logger.LogInformation($"{package} already installed");
                continue;
            }

            try
            {
                Logger.LogInformation($"Installing {package}...");
                await RunAsync(pythonExe, $"-m pip install --upgrade {package}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to install {package}: {ex.Message}");
            }
        }
    }

    private static async Task ConfigureToolPathsAsync(string venvPath, string pythonExe)
    {
        var ytdlp = GetVenvExecutable(venvPath, "yt-dlp");
        if (File.Exists(ytdlp))
        {
            ConfigService.Config.YtdlpLocation = ytdlp;
            Logger.LogInformation($"Using yt-dlp at {ytdlp}");
        }

        var ffmpeg = await GetFfmpegPathAsync(pythonExe);
        if (ffmpeg is not null)
        {
            ConfigService.Config.FfmpegLocation = ffmpeg;
            Logger.LogInformation($"Using ffmpeg at {ffmpeg}");
        }
    }

    private static async Task<bool> IsInstalledAsync(string pythonExe, string package)
    {
        var exitCode = await RunAsync(pythonExe, $"-m pip show {package}");
        return exitCode == 0;
    }

    private static async Task<string?> GetFfmpegPathAsync(string pythonExe)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-c \"import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null) return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var path = output.Trim();
        return File.Exists(path) ? path : null;
    }

    private static async Task<int> RunAsync(string pythonExe, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string GetVenvExecutable(string venvPath, string name)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(venvPath, "Scripts", $"{name}.exe");
        }

        return Path.Combine(venvPath, "bin", name);
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Desktop.Exceptions.WhisperService;
using Desktop.Exceptions.FfmpegException;
using Desktop.Exceptions.YouTubeService.YtdlpException;

namespace Desktop.Services;

public static class EnvironmentService
{
    private static readonly ILogger Logger = new Logger("Environment");

    /// <summary>
    /// Ensures the virtual environment exists and is updated with necessary packages.
    /// </summary>
    public static async Task EnsureVirtualEnvAsync()
    {
        string venvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".venv");
        
        if (!Directory.Exists(venvPath))
        {
            Logger.LogInformation("Creating virtual environment in .venv...");
            CreateVenv(venvPath);
        }

        await InstallDependenciesAsync(venvPath);
    }

    private static void CreateVenv(string venvPath)
    {
        try
        {
            // Use the system python to create a venv. 
            // This assumes 'python' or 'python3' is in PATH.
            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
            ProcessStartInfo startInfo = new ProcessStartInfo
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
                throw new Exception($"Failed to create virtual environment. Ensure python is installed and in PATH.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating venv: {ex.Message}");
            throw;
        }
    }

    private static async Task InstallDependenciesAsync(string venvPath)
    {
        string pythonExe = GetPythonExecutable(venvPath);
        Logger.LogInformation($"Installing dependencies using {pythonExe}...");

        var packages = new[] { "openai-whisper", "torch", "torchvision", "torchaudio" };
        foreach (var package in packages)
        {
            try
            {
                await RunPipInstallAsync(pythonExe, $"-m pip install --upgrade {package}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to install {package}: {ex.Message}");
            }
        }
    }

    private static async Task RunPipInstallAsync(string pythonExe, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{arguments}\"",
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
    }

    private static string GetPythonExecutable(string venvPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(venvPath, "Scripts", "python.exe");
        }
        else
        {
            return Path.Combine(venvPath, "bin", "python");
        }
    }
}

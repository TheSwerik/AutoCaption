using System;
using Avalonia;
using Desktop.Services;

namespace Desktop;

internal sealed class Program
{
    private static readonly ILogger Logger = new Logger<Program>();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigService.Init(args);
        try
        {
            EnvironmentService.EnsureVirtualEnvAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Environment setup failed: {ex.Message}");
        }

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
}
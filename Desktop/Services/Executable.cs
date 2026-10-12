using System;
using System.Runtime.InteropServices;

namespace Desktop.Services;

public static class Executable
{
    public static string GetExecutable(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return path;

        return !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? $"{path}.exe" : path;
    }
}
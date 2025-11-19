using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Desktop.Services;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class FileRevealer
{
    [LibraryImport("shell32.dll", SetLastError = true)]
    private static partial int SHOpenFolderAndSelectItems(
        IntPtr pidlFolder,
        uint cidl,
        IntPtr[] apidl,
        uint dwFlags);

    // [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    // private static partial int SHParseDisplayName(
    //     string name,
    //     IntPtr bindingContext,
    //     out IntPtr ppidl,
    //     uint sfgaoIn,
    //     out uint psfgaoOut);

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(
        string pszName,
        IntPtr pbc,
        out IntPtr ppidl,
        uint sfgaoIn,
        out uint psfgaoOut);


    public static void Reveal(string filePath)
    {
        IntPtr pidl;
        var hr = SHParseDisplayName(filePath, IntPtr.Zero, out pidl, 0, out _);
        if (hr != 0) return;

        SHOpenFolderAndSelectItems(pidl, 0, null, 0);
    }
}
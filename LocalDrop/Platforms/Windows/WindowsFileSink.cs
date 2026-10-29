using System.Diagnostics;
using System.Runtime.InteropServices;
using LocalDrop.Services;

namespace LocalDrop;

public sealed class WindowsFileSink : IFileSink
{
    public string Root { get; }
    public string RootDescription => Root;

    public WindowsFileSink()
    {
        Root = Path.Combine(GetDownloadsFolder(), "LocalDrop");
    }

    public async Task<string> SaveAsync(string sender, string fileName, Stream content, long length, IProgress<long>? progress, CancellationToken ct)
    {
        var dir = Path.Combine(Root, sender);
        Directory.CreateDirectory(dir);
        var name = StreamCopy.UniqueName(fileName, n => File.Exists(Path.Combine(dir, n)) || File.Exists(Path.Combine(dir, n + ".part")));
        var final = Path.Combine(dir, name);
        var temp = final + ".part";
        try
        {
            await using (var fs = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
            {
                await StreamCopy.CopyAsync(content, fs, length, progress, ct);
            }
            File.Move(temp, final);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
        return final;
    }

    public Task OpenRootAsync()
    {
        Directory.CreateDirectory(Root);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Root}\"") { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private static string GetDownloadsFolder()
    {
        try
        {
            var guid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            if (SHGetKnownFolderPath(guid, 0, IntPtr.Zero, out var path) == 0 && !string.IsNullOrEmpty(path))
                return path;
        }
        catch { }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = true)]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, [MarshalAs(UnmanagedType.LPWStr)] out string pszPath);
}

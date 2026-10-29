using System.Diagnostics;
using LocalDrop.Services;
using Microsoft.Toolkit.Uwp.Notifications;

namespace LocalDrop;

public sealed class WindowsNotifier : INotifier
{
    private static bool _hooked;

    public WindowsNotifier()
    {
        if (_hooked) return;
        _hooked = true;
        ToastNotificationManagerCompat.OnActivated += e =>
        {
            var args = ToastArguments.Parse(e.Argument);
            if (args.TryGetValue("open", out var path) && !string.IsNullOrEmpty(path))
            {
                try
                {
                    if (File.Exists(path))
                        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                    else if (Directory.Exists(path))
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
                }
                catch { }
            }
            else
            {
                PlatformSetup.ShowMainWindow();
            }
        };
    }

    public Task NotifyReceivedAsync(ReceivedFile file)
    {
        try
        {
            new ToastContentBuilder()
                .AddArgument("open", file.DisplayPath)
                .AddText(Strings.Get("NotifTitle"))
                .AddText(Strings.Format("NotifBody", file.FileName, file.Sender))
                .AddButton(new ToastButton()
                    .SetContent(Strings.Get("OpenFolder"))
                    .AddArgument("open", Path.GetDirectoryName(file.DisplayPath) ?? file.DisplayPath))
                .Show();
        }
        catch
        {
            // Notifications are best-effort.
        }
        return Task.CompletedTask;
    }
}

using LocalDrop.Services;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace LocalDrop;

public static partial class PlatformSetup
{
    /// <summary>The main window's AppWindow, used by the tray icon to show/hide.</summary>
    public static AppWindow? MainAppWindow { get; private set; }
    public static Microsoft.UI.Xaml.Window? NativeWindow { get; private set; }

    public static partial void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IFileSink, WindowsFileSink>();
        services.AddSingleton<INotifier, WindowsNotifier>();
        services.AddSingleton<IBackgroundKeeper, NoopBackgroundKeeper>();
    }

    public static partial void ConfigureLifecycle(ILifecycleBuilder events)
    {
        events.AddWindows(w => w.OnWindowCreated(window =>
        {
            NativeWindow = window;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(id);
            MainAppWindow = appWindow;

            // Close button hides to tray instead of quitting; "Quit" in the tray menu really exits.
            appWindow.Closing += (_, e) =>
            {
                if (QuitRequested) return;
                e.Cancel = true;
                appWindow.Hide();
            };

            // Start hidden when launched with --minimized (autostart).
            if (Environment.GetCommandLineArgs().Contains("--minimized", StringComparer.OrdinalIgnoreCase))
            {
                window.DispatcherQueue.TryEnqueue(() => appWindow.Hide());
            }
        }));
    }

    public static bool QuitRequested { get; set; }

    public static void ShowMainWindow()
    {
        if (MainAppWindow is null || NativeWindow is null) return;
        NativeWindow.DispatcherQueue.TryEnqueue(() =>
        {
            MainAppWindow.Show(true);
            NativeWindow.Activate();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(NativeWindow);
            SetForegroundWindow(hwnd);
        });
    }

    public static void Quit()
    {
        QuitRequested = true;
        Application.Current?.Quit();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

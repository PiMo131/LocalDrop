using Microsoft.UI.Xaml;

namespace LocalDrop.WinUI;

public partial class App : MauiWinUIApplication
{
    public static readonly string LogPath = Path.Combine(Path.GetTempPath(), "LocalDrop.log");

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => { Log("WinUI UnhandledException", e.Exception); };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("AppDomain UnhandledException", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => { Log("UnobservedTaskException", e.Exception); e.SetObserved(); };
    }

    protected override MauiApp CreateMauiApp()
    {
        try { return MauiProgram.CreateMauiApp(); }
        catch (Exception ex) { Log("CreateMauiApp", ex); throw; }
    }

    public static void Log(string where, Exception? ex)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {where}: {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }
}

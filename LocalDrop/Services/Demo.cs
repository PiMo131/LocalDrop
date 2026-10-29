using System.Net;

namespace LocalDrop.Services;

/// <summary>
/// Demo mode for screenshots/documentation: shows fixed fake addresses, host name and folder instead of real ones.
/// Enable with environment variable LOCALDROP_DEMO=1 (desktop) or `adb shell setprop debug.localdrop.demo 1` (Android).
/// Uploads still work normally.
/// </summary>
public static partial class Demo
{
    private static bool? _enabled;

    public static bool IsEnabled => _enabled ??= Environment.GetEnvironmentVariable("LOCALDROP_DEMO") == "1" || PlatformCheck();

    public static IReadOnlyList<IPAddress> Addresses { get; } = new[] { IPAddress.Parse("192.168.1.23") };
    public static string HostName => DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? "Telefoon" : "Laptop";
    public static string WindowsRoot => @"C:\Users\jij\Downloads\LocalDrop";

    static partial void PlatformCheckImpl(ref bool result);

    private static bool PlatformCheck()
    {
        bool r = false;
        PlatformCheckImpl(ref r);
        return r;
    }
}

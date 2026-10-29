namespace LocalDrop.Services;

public static partial class Demo
{
    static partial void PlatformCheckImpl(ref bool result)
    {
        try
        {
            // android.os.SystemProperties.get("debug.localdrop.demo") via reflection (hidden API, read-only use).
            var cls = Java.Lang.Class.ForName("android.os.SystemProperties");
            var method = cls.GetMethod("get", Java.Lang.Class.FromType(typeof(Java.Lang.String)));
            var value = method?.Invoke(null, "debug.localdrop.demo")?.ToString();
            result = value?.Trim() == "1";
        }
        catch { }
    }
}

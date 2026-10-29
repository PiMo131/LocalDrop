using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using AndroidX.Core.App;
using LocalDrop.Services;

namespace LocalDrop;

/// <summary>Foreground service that keeps the process (and thus the embedded web server) alive and the Wi-Fi awake.</summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class DropService : Service
{
    public const string ExtraUrl = "url";
    private const int NotificationId = 1;

    private WifiManager.WifiLock? _wifiLock;
    private PowerManager.WakeLock? _wakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        AndroidNotifier.EnsureChannels(this);
        var url = intent?.GetStringExtra(ExtraUrl) ?? "";

        var launch = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pi = launch is null ? null : PendingIntent.GetActivity(this, 0, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new NotificationCompat.Builder(this, AndroidNotifier.ServiceChannel)
            .SetSmallIcon(Resource.Drawable.notify_icon)
            .SetContentTitle("LocalDrop")
            .SetContentText(Strings.Format("ServiceNotif", url))
            .SetOngoing(true)
            .SetContentIntent(pi)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
            StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
        else
            StartForeground(NotificationId, notification);

        AcquireLocks();
        return StartCommandResult.Sticky;
    }

    private void AcquireLocks()
    {
        try
        {
            if (_wifiLock is null)
            {
                var wifi = (WifiManager?)ApplicationContext?.GetSystemService(WifiService);
                _wifiLock = wifi?.CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "LocalDrop:wifi");
                _wifiLock?.Acquire();
            }
            if (_wakeLock is null)
            {
                var pm = (PowerManager?)GetSystemService(PowerService);
                _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "LocalDrop:wake");
                _wakeLock?.Acquire();
            }
        }
        catch { }
    }

    public override void OnDestroy()
    {
        try { if (_wifiLock?.IsHeld == true) _wifiLock.Release(); } catch { }
        try { if (_wakeLock?.IsHeld == true) _wakeLock.Release(); } catch { }
        _wifiLock = null;
        _wakeLock = null;
        base.OnDestroy();
    }
}

public sealed class AndroidBackgroundKeeper : IBackgroundKeeper
{
    public void Start(string url)
    {
        try
        {
            var ctx = Platform.AppContext;
            var intent = new Intent(ctx, typeof(DropService));
            intent.PutExtra(DropService.ExtraUrl, url);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) ctx.StartForegroundService(intent);
            else ctx.StartService(intent);
        }
        catch { }
    }

    public void Stop()
    {
        try
        {
            var ctx = Platform.AppContext;
            ctx.StopService(new Intent(ctx, typeof(DropService)));
        }
        catch { }
    }
}

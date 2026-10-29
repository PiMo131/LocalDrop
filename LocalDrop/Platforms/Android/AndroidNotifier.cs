using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using LocalDrop.Services;

namespace LocalDrop;

public sealed class AndroidNotifier : INotifier
{
    public const string ReceivedChannel = "localdrop_received";
    public const string ServiceChannel = "localdrop_service";
    private static int _id = 100;

    public static void EnsureChannels(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        if (nm is null) return;

        nm.CreateNotificationChannel(new NotificationChannel(ReceivedChannel, Strings.Get("NotifTitle"), NotificationImportance.High)
        {
            Description = "LocalDrop",
        });
        nm.CreateNotificationChannel(new NotificationChannel(ServiceChannel, "LocalDrop", NotificationImportance.Low)
        {
            Description = "LocalDrop",
        });
    }

    public Task NotifyReceivedAsync(ReceivedFile file)
    {
        try
        {
            var ctx = Platform.AppContext;
            EnsureChannels(ctx);

            var open = new Intent(DownloadManager.ActionViewDownloads);
            open.AddFlags(ActivityFlags.NewTask);
            var pi = PendingIntent.GetActivity(ctx, 0, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var n = new NotificationCompat.Builder(ctx, ReceivedChannel)
                .SetSmallIcon(Resource.Drawable.notify_icon)
                .SetContentTitle(Strings.Get("NotifTitle"))
                .SetContentText(Strings.Format("NotifBody", file.FileName, file.Sender))
                .SetAutoCancel(true)
                .SetContentIntent(pi)
                .SetPriority(NotificationCompat.PriorityHigh)
                .Build();

            NotificationManagerCompat.From(ctx).Notify(Interlocked.Increment(ref _id), n);
        }
        catch { }
        return Task.CompletedTask;
    }
}

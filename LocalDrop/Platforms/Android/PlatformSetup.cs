using LocalDrop.Services;
using Microsoft.Maui.LifecycleEvents;

namespace LocalDrop;

public static partial class PlatformSetup
{
    public static partial void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IFileSink, AndroidFileSink>();
        services.AddSingleton<INotifier, AndroidNotifier>();
        services.AddSingleton<IBackgroundKeeper, AndroidBackgroundKeeper>();
    }

    public static partial void ConfigureLifecycle(ILifecycleBuilder events)
    {
        // Nothing platform-specific needed on Android beyond MainActivity.
    }
}

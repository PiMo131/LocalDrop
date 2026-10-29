using LocalDrop.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace LocalDrop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events => PlatformSetup.ConfigureLifecycle(events));

        PlatformSetup.RegisterServices(builder.Services);

        builder.Services.AddSingleton<DropServer>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}

/// <summary>Per-platform DI + lifecycle wiring; bodies live in Platforms/*/PlatformSetup.cs.</summary>
public static partial class PlatformSetup
{
    public static partial void RegisterServices(IServiceCollection services);
    public static partial void ConfigureLifecycle(ILifecycleBuilder events);
}

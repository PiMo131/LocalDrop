namespace LocalDrop.Services;

/// <summary>Keeps the process alive while the server runs (Android foreground service; no-op on desktop).</summary>
public interface IBackgroundKeeper
{
    void Start(string url);
    void Stop();
}

public sealed class NoopBackgroundKeeper : IBackgroundKeeper
{
    public void Start(string url) { }
    public void Stop() { }
}

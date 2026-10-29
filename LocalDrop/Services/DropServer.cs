using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using EmbedIO;
using EmbedIO.Actions;


namespace LocalDrop.Services;

/// <summary>
/// The LAN web server: serves the drop page, accepts PUT /upload, refuses non-LAN clients,
/// requires a 4-digit session PIN for uploads.
/// </summary>
public sealed class DropServer : IDisposable
{
    public const int DefaultPort = 8787;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly IFileSink _sink;
    private readonly INotifier _notifier;
    private WebServer? _server;
    private CancellationTokenSource? _cts;
    private string _pageHtml = "";

    // Brute-force guard: ip -> (failures, lockedUntil)
    private readonly ConcurrentDictionary<string, (int failures, DateTime lockedUntil)> _pinFailures = new();

    public DropServer(IFileSink sink, INotifier notifier)
    {
        _sink = sink;
        _notifier = notifier;
    }

    public bool IsRunning => _server is { State: WebServerState.Listening };
    public int Port { get; private set; } = DefaultPort;
    public string Pin { get; private set; } = "0000";
    public IReadOnlyList<IPAddress> Addresses { get; private set; } = Array.Empty<IPAddress>();
    public string PrimaryUrl => Addresses.Count > 0 ? $"http://{Addresses[0]}:{Port}/" : $"http://localhost:{Port}/";
    public string PrimaryUrlWithPin => PrimaryUrl + "?pin=" + Pin;

    public event Action<ReceivedFile>? FileReceived;
    public event Action<string, double>? Progress; // fileName, 0..1
    public event Action? StateChanged;

    public async Task StartAsync()
    {
        if (IsRunning) return;

        if (string.IsNullOrEmpty(_pageHtml))
        {
            using var s = await FileSystem.OpenAppPackageFileAsync("drop.html");
            using var r = new StreamReader(s, Utf8NoBom);
            _pageHtml = await r.ReadToEndAsync();
        }

        Pin = RandomNumberGenerator.GetInt32(0, 10000).ToString("0000");
#if DEBUG
        try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "LocalDrop.pin"), Pin); } catch { }
#endif
        Port = NetUtil.FindFreePort(DefaultPort);
        Addresses = NetUtil.LocalIPv4Addresses();
        _pinFailures.Clear();

        _cts = new CancellationTokenSource();
        _server = new WebServer(o => o
                .WithUrlPrefix($"http://*:{Port}/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule("/", HttpVerbs.Any, HandleAsync));
        _server.StateChanged += (_, _) => StateChanged?.Invoke();
        _ = _server.RunAsync(_cts.Token);

        // Give the listener a moment to come up so IsRunning is accurate for the UI.
        for (int i = 0; i < 20 && !IsRunning; i++) await Task.Delay(50);
        StateChanged?.Invoke();
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _server?.Dispose(); } catch { }
        _server = null;
        _cts = null;
        StateChanged?.Invoke();
    }

    public void RefreshAddresses()
    {
        Addresses = NetUtil.LocalIPv4Addresses();
        StateChanged?.Invoke();
    }

    private async Task HandleAsync(IHttpContext ctx)
    {
        var remote = ctx.Request.RemoteEndPoint?.Address;
        if (!NetUtil.IsLan(remote))
        {
            ctx.Response.StatusCode = 403;
            await ctx.SendStringAsync("Forbidden: LAN only", "text/plain", Utf8NoBom);
            return;
        }

        var path = ctx.RequestedPath;
        var verb = ctx.Request.HttpVerb;

        if (verb == HttpVerbs.Get && (path == "/" || path == "/index.html"))
        {
            ctx.Response.Headers["Cache-Control"] = "no-store";
            await ctx.SendStringAsync(_pageHtml, "text/html", Utf8NoBom);
            return;
        }

        if (verb == HttpVerbs.Get && path == "/info")
        {
            ctx.Response.Headers["Cache-Control"] = "no-store";
            await ctx.SendDataAsync(new { host = DeviceInfo.Current.Name, port = Port });
            return;
        }

        if (verb == HttpVerbs.Put && path == "/upload")
        {
            await HandleUploadAsync(ctx, remote!);
            return;
        }

        ctx.Response.StatusCode = 404;
        await ctx.SendStringAsync("Not found", "text/plain", Utf8NoBom);
    }

    private async Task HandleUploadAsync(IHttpContext ctx, IPAddress remote)
    {
        var ip = remote.ToString();
        if (_pinFailures.TryGetValue(ip, out var st) && st.lockedUntil > DateTime.UtcNow)
        {
            ctx.Response.StatusCode = 429;
            await ctx.SendStringAsync("Too many attempts", "text/plain", Utf8NoBom);
            return;
        }

        var pin = ctx.Request.Headers["X-Pin"] ?? "";
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(pin.PadRight(4)[..4]), Encoding.ASCII.GetBytes(Pin)))
        {
            var cur = _pinFailures.GetOrAdd(ip, _ => (0, DateTime.MinValue));
            var failures = cur.failures + 1;
            _pinFailures[ip] = failures >= 5 ? (0, DateTime.UtcNow.AddSeconds(60)) : (failures, DateTime.MinValue);
            ctx.Response.StatusCode = 401;
            await ctx.SendStringAsync("Wrong code", "text/plain", Utf8NoBom);
            return;
        }
        _pinFailures.TryRemove(ip, out _);

        var rawSender = ctx.Request.Headers["X-Sender"];
        if (!string.IsNullOrEmpty(rawSender))
        {
            try { rawSender = Uri.UnescapeDataString(rawSender); } catch { }
        }
        var sender = NetUtil.SanitizeSegment(rawSender, ip);
        var fileName = NetUtil.SanitizeFileName(ctx.Request.QueryString["name"]);
        var length = ctx.Request.ContentLength64;

        var progress = new Progress<long>(done =>
        {
            if (length > 0) Progress?.Invoke(fileName, Math.Min(1.0, done / (double)length));
        });

        string displayPath;
        try
        {
            displayPath = await _sink.SaveAsync(sender, fileName, ctx.Request.InputStream, length, progress, ctx.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            await ctx.SendStringAsync("Save failed: " + ex.Message, "text/plain", Utf8NoBom);
            return;
        }

        var finalName = Path.GetFileName(displayPath.Replace('\\', '/'));
        if (string.IsNullOrEmpty(finalName)) finalName = fileName;
        var received = new ReceivedFile(sender, finalName, length, displayPath, DateTime.Now);
        FileReceived?.Invoke(received);
        _ = _notifier.NotifyReceivedAsync(received);

        await ctx.SendDataAsync(new { ok = true, path = displayPath });
    }

    public void Dispose() => Stop();
}

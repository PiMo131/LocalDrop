using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalDrop.Services;
using QRCoder;

namespace LocalDrop;

public partial class MainViewModel : ObservableObject
{
    private readonly DropServer _server;
    private readonly IFileSink _sink;
    private readonly IBackgroundKeeper _keeper;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _toggleText = "";
    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _otherUrls = "";
    [ObservableProperty] private bool _hasNetwork = true;
    [ObservableProperty] private ImageSource? _qrImage;
    [ObservableProperty] private string _saveLocation = "";
    [ObservableProperty] private string _toast = "";
    [ObservableProperty] private bool _hasReceived;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isTransferring;

    public ObservableCollection<ReceivedItem> Received { get; } = new();

    public MainViewModel(DropServer server, IFileSink sink, IBackgroundKeeper keeper)
    {
        _server = server;
        _sink = sink;
        _keeper = keeper;
        SaveLocation = sink.RootDescription;

        _server.StateChanged += () => MainThread.BeginInvokeOnMainThread(Refresh);
        _server.FileReceived += f => MainThread.BeginInvokeOnMainThread(() =>
        {
            Received.Insert(0, new ReceivedItem(f));
            HasReceived = true;
            IsTransferring = false;
            Progress = 0;
            ProgressText = "";
        });
        _server.Progress += (name, p) => MainThread.BeginInvokeOnMainThread(() =>
        {
            IsTransferring = p < 1;
            Progress = p;
            ProgressText = $"{name}  {p:P0}";
        });

        Connectivity.Current.ConnectivityChanged += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            _server.RefreshAddresses();
        });

        Refresh();
    }

    public async Task StartAsync()
    {
        await _server.StartAsync();
        _keeper.Start(_server.PrimaryUrl);
        Refresh();
    }

    public void Refresh()
    {
        IsRunning = _server.IsRunning;
        StatusText = Strings.Get(IsRunning ? "Running" : "Stopped");
        ToggleText = Strings.Get(IsRunning ? "Stop" : "Start");
        Pin = _server.Pin;
        HasNetwork = _server.Addresses.Count > 0;
        Url = _server.PrimaryUrl;
        OtherUrls = _server.Addresses.Count > 1
            ? string.Join("   ", _server.Addresses.Skip(1).Select(a => $"http://{a}:{_server.Port}/"))
            : "";
        QrImage = IsRunning && HasNetwork ? MakeQr(_server.PrimaryUrlWithPin) : null;
    }

    private static ImageSource MakeQr(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(12, new byte[] { 0x0f, 0x17, 0x2a }, new byte[] { 0xff, 0xff, 0xff }, true);
        return ImageSource.FromStream(() => new MemoryStream(png));
    }

    [RelayCommand]
    private async Task Toggle()
    {
        if (_server.IsRunning)
        {
            _server.Stop();
            _keeper.Stop();
        }
        else
        {
            await StartAsync();
        }
        Refresh();
    }

    [RelayCommand]
    private async Task NewPin()
    {
        _server.Stop();
        await StartAsync();
    }

    [RelayCommand]
    private void RefreshNetwork()
    {
        _server.RefreshAddresses();
        Refresh();
    }

    [RelayCommand]
    private Task OpenFolder() => _sink.OpenRootAsync();

    [RelayCommand]
    private async Task CopyLink()
    {
        await Clipboard.Default.SetTextAsync(_server.PrimaryUrlWithPin);
        Toast = Strings.Get("Copied");
        await Task.Delay(1500);
        Toast = "";
    }

    [RelayCommand]
    private async Task OpenInBrowser()
    {
        try { await Launcher.Default.OpenAsync(new Uri(_server.PrimaryUrlWithPin)); } catch { }
    }
}

public sealed class ReceivedItem
{
    public ReceivedItem(ReceivedFile f)
    {
        FileName = f.FileName;
        Detail = $"{Fmt(f.Size)}  ·  {Strings.From} {f.Sender}  ·  {f.ReceivedAt:HH:mm}";
        Path = f.DisplayPath;
    }
    public string FileName { get; }
    public string Detail { get; }
    public string Path { get; }

    private static string Fmt(long b) => b switch
    {
        < 1024 => $"{b} B",
        < 1048576 => $"{b / 1024.0:0} KB",
        < 1073741824 => $"{b / 1048576.0:0.0} MB",
        _ => $"{b / 1073741824.0:0.00} GB",
    };
}

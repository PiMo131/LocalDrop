namespace LocalDrop;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private bool _started;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        InitPlatform();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        await _vm.StartAsync();
    }

    /// <summary>Platform hook (tray icon on Windows). Implemented in Platforms/*.</summary>
    partial void InitPlatform();
}

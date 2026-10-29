namespace LocalDrop;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_mainPage)
        {
            Title = "LocalDrop",
            Width = 620,
            Height = 760,
            MinimumWidth = 420,
            MinimumHeight = 560,
        };
        return window;
    }
}

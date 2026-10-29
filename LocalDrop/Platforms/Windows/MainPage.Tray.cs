using H.NotifyIcon;
using LocalDrop.Services;

namespace LocalDrop;

public partial class MainPage
{
    private TaskbarIcon? _tray;

    partial void InitPlatform()
    {
        var menu = new MenuFlyout();
        var show = new MenuFlyoutItem { Text = Strings.Show };
        show.Clicked += (_, _) => PlatformSetup.ShowMainWindow();
        var open = new MenuFlyoutItem { Text = Strings.OpenFolder };
        open.Clicked += async (_, _) => await _vm.OpenFolderCommand.ExecuteAsync(null);
        var quit = new MenuFlyoutItem { Text = Strings.Quit };
        quit.Clicked += (_, _) => PlatformSetup.Quit();
        menu.Add(show);
        menu.Add(open);
        menu.Add(new MenuFlyoutSeparator());
        menu.Add(quit);

        _tray = new TaskbarIcon
        {
            ToolTipText = "LocalDrop",
            IconSource = "trayicon.ico",
            ContextMenuMode = ContextMenuMode.PopupMenu,
            LeftClickCommand = new Command(() => PlatformSetup.ShowMainWindow()),
        };
        FlyoutBase.SetContextFlyout(_tray, menu);
        _tray.ForceCreate();
        Loaded += (_, _) => { if (!(_tray?.IsCreated ?? true)) _tray?.ForceCreate(); };
        Unloaded += (_, _) => { };
    }
}

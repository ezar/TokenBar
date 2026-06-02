using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TokenBar.App;

public partial class App : Application
{
    private MainWindow? mainWindow;
    private MiniWindow? miniWindow;
    private TrayIcon? trayIcon;
    private bool isQuitting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            mainWindow = new MainWindow();
            miniWindow = new MiniWindow(mainWindow.ViewModel);
            mainWindow.Closing += MainWindowClosing;
            desktop.MainWindow = mainWindow;
            trayIcon = CreateTrayIcon(desktop, mainWindow, miniWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private TrayIcon CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window, MiniWindow mini)
    {
        var showItem = new NativeMenuItem("Mostrar TokenBar");
        showItem.Click += (_, _) => ShowMainWindow(window, mini);

        var miniItem = new NativeMenuItem("Mostrar miniatura");
        miniItem.Click += (_, _) => ShowMiniWindow(window, mini);

        var refreshItem = new NativeMenuItem("Actualizar ahora");
        refreshItem.Click += async (_, _) => await window.RefreshNowAsync(CancellationToken.None);

        var exitItem = new NativeMenuItem("Salir");
        exitItem.Click += (_, _) =>
        {
            isQuitting = true;
            trayIcon?.Dispose();
            mini.ForceClose();
            window.Close();
            desktop.Shutdown();
        };

        var menu = new NativeMenu
        {
            Items =
            {
                showItem,
                miniItem,
                refreshItem,
                new NativeMenuItemSeparator(),
                exitItem
            }
        };

        var icon = new TrayIcon
        {
            Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "tokenbar.ico")),
            ToolTipText = "TokenBar",
            Menu = menu,
            IsVisible = true
        };
        icon.Clicked += (_, _) => ShowMiniWindow(window, mini);

        return icon;
    }

    private void MainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (isQuitting || mainWindow is null)
        {
            return;
        }

        e.Cancel = true;
        mainWindow.Hide();
    }

    private static void ShowMainWindow(MainWindow window)
    {
        window.Show();
        window.Activate();
    }

    private static void ShowMainWindow(MainWindow window, MiniWindow mini)
    {
        mini.Hide();
        ShowMainWindow(window);
    }

    private static void ShowMiniWindow(MainWindow window, MiniWindow mini)
    {
        window.Hide();
        mini.ShowAtBottomRight();
    }
}

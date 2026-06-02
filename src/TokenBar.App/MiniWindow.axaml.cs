using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TokenBar.App.ViewModels;

namespace TokenBar.App;

public partial class MiniWindow : Window
{
    private bool allowClose;

    public MiniWindow()
        : this(MainWindow.CreateDefaultViewModel())
    {
    }

    public MiniWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Opened += MiniWindowOpened;
        Closing += MiniWindowClosing;
    }

    public void ShowAtBottomRight()
    {
        Show();
        PositionAtBottomRight();
        Activate();
    }

    private void MiniWindowOpened(object? sender, EventArgs e)
    {
        PositionAtBottomRight();
    }

    public void ForceClose()
    {
        allowClose = true;
        Close();
    }

    private void MiniWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void HideClicked(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void WindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void PositionAtBottomRight()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var margin = 16;
        var x = screen.WorkingArea.X + screen.WorkingArea.Width - (int)Width - margin;
        var y = screen.WorkingArea.Y + screen.WorkingArea.Height - (int)Height - margin;
        Position = new PixelPoint(Math.Max(screen.WorkingArea.X, x), Math.Max(screen.WorkingArea.Y, y));
    }
}

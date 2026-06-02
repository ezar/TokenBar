using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TokenBar.App.ViewModels;
using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Providers.BuiltIn;
using TokenBar.Core.Refresh;

namespace TokenBar.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly DispatcherTimer refreshTimer;

    public MainWindow()
        : this(CreateDefaultViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;
        refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        refreshTimer.Tick += RefreshTimerTick;
        InitializeComponent();
        Loaded += MainWindowLoaded;
        Closed += MainWindowClosed;
    }

    public MainWindowViewModel ViewModel => viewModel;

    internal static MainWindowViewModel CreateDefaultViewModel()
    {
        var config = TokenBarConfig.CreateDefault();
        var registry = new ProviderRegistry(BuiltInProviderFactory.CreateProviders());
        var refreshService = new RefreshService(registry);
        return new MainWindowViewModel(refreshService, config);
    }

    private async void MainWindowLoaded(object? sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync(CancellationToken.None);
        refreshTimer.Start();
    }

    private async void RefreshClicked(object? sender, RoutedEventArgs e)
    {
        await RefreshNowAsync(CancellationToken.None);
    }

    public async Task RefreshNowAsync(CancellationToken cancellationToken)
    {
        await viewModel.RefreshAsync(cancellationToken);
    }

    private async void RefreshTimerTick(object? sender, EventArgs e)
    {
        if (viewModel.NextRefreshDueAt is not { } dueAt)
        {
            return;
        }

        if (DateTimeOffset.UtcNow >= dueAt)
        {
            await viewModel.RefreshAsync(CancellationToken.None);
            return;
        }

        viewModel.UpdateCountdown(DateTimeOffset.UtcNow);
    }

    private void MainWindowClosed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
    }
}

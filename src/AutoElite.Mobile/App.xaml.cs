using AutoElite.Mobile.Offline;

namespace AutoElite.Mobile;

/// <summary>
/// The application, with one window that shows the shell; it sends the actions taken without a network when it starts
/// and when it comes back to the foreground.
/// </summary>
public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly SynchronizationTrigger _synchronization;

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="shell">The shell of the pages.</param>
    /// <param name="synchronization">Sends the actions taken without a network.</param>
    public App(AppShell shell, SynchronizationTrigger synchronization)
    {
        InitializeComponent();

        // Noir Modern is dark: the system controls follow it whatever the theme of the device.
        UserAppTheme = AppTheme.Dark;
        _shell = shell;
        _synchronization = synchronization;
    }

    /// <inheritdoc />
    protected override void OnStart() => _synchronization.Start();

    /// <inheritdoc />
    protected override void OnResume() => _synchronization.Synchronize();

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState) => new(_shell) { Title = "AutoElite" };
}

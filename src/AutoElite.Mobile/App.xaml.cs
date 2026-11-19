using AutoElite.Mobile.Offline;
using AutoElite.Mobile.Theming;

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

        // The dark or the light colors of Noir Modern, as the user chose or as the device is.
        AppPalette.Start(this);
        _shell = shell;
        _synchronization = synchronization;
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        AppPalette.ApplyToSystemBars();
        _synchronization.Start();
    }

    /// <inheritdoc />
    protected override void OnResume() => _synchronization.Synchronize();

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState) => new(_shell) { Title = "AutoElite" };
}

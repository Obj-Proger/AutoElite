namespace AutoElite.Mobile;

/// <summary>The application, with one window that shows the shell.</summary>
public partial class App : Application
{
    private readonly AppShell _shell;

    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    /// <param name="shell">The shell of the pages.</param>
    public App(AppShell shell)
    {
        InitializeComponent();
        _shell = shell;
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState) => new(_shell) { Title = "AutoElite" };
}

namespace AutoElite.Mobile.WinUI;

/// <summary>The application on Windows, which creates the MAUI application.</summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    public App() => InitializeComponent();

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

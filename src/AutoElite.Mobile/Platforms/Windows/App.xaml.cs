namespace AutoElite.Mobile.WinUI;

/// <summary>The application on Windows, which creates the MAUI application.</summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>Initializes a new instance of the <see cref="App"/> class.</summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // When a window of the system opens over the application, such as the choice of an application for a tel link,
    // Windows may report the window of the application deactivated twice, and MAUI throws on the second report. The
    // window is deactivated either way, so that exception of the MAUI window is handled rather than closing the
    // application; every other exception stays unhandled.
    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is InvalidOperationException exception
            && exception.TargetSite?.DeclaringType == typeof(Microsoft.Maui.Controls.Window)
            && exception.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase))
        {
            e.Handled = true;
        }
    }
}

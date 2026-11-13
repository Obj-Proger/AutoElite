using Android.App;
using Android.Runtime;

namespace AutoElite.Mobile;

/// <summary>The application on Android, which creates the MAUI application.</summary>
/// <param name="handle">The handle of the Java object.</param>
/// <param name="ownership">Who owns the handle.</param>
[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

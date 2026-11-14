using Android.App;
using Android.Runtime;

namespace AutoElite.Mobile;

/// <summary>The application on Android, which creates the MAUI application.</summary>
/// <param name="handle">The handle of the Java object.</param>
/// <param name="ownership">Who owns the handle.</param>
/// <remarks>
/// Only a debug build may call the API of the development machine over HTTP; a release build allows no HTTP at all.
/// </remarks>
#if DEBUG
[Application(NetworkSecurityConfig = "@xml/network_security_config_debug")]
#else
[Application(NetworkSecurityConfig = "@xml/network_security_config")]
#endif
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

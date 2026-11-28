namespace AutoElite.Application.Notifications.RegisterPushDevice;

/// <summary>
/// Registers the device of the signed-in user for push notifications, as the application does after signing in and
/// whenever the push service gives it a new token. A token already registered, by this user or another one signed in
/// on the same device before, passes to this user.
/// </summary>
/// <param name="Token">The token the push service gave the application on the device.</param>
/// <param name="Platform">The push service.</param>
public sealed record RegisterPushDeviceCommand(string Token, PushPlatform Platform) : ICommand;

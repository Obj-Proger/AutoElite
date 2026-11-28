namespace AutoElite.Application.Notifications.UnregisterPushDevice;

/// <summary>
/// Stops the push notifications of the signed-in user on a device, as the application does when the user signs out
/// there. A token that is not theirs, or not registered, is left as it is, and the request still succeeds, so that
/// signing out never fails and tells nothing about the devices of others.
/// </summary>
/// <param name="Token">The token of the device in the push service.</param>
public sealed record UnregisterPushDeviceCommand(string Token) : ICommand;

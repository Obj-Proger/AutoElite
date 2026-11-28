namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Sends push notifications to devices through their push service. Sending is best effort: a notification that does
/// not reach a device stays among the notifications of the user in the application.
/// </summary>
public interface IPushSender
{
    /// <summary>Sends a push notification to devices.</summary>
    /// <param name="tokens">The tokens of the devices in the push service.</param>
    /// <param name="message">The title, the text and the data of the notification.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The tokens the push service no longer knows, whose devices are to be removed.</returns>
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken = default);
}

/// <summary>A push notification: what the device shows and what the application reads when it is tapped.</summary>
/// <param name="Title">The title shown by the device.</param>
/// <param name="Body">The text shown by the device, if any.</param>
/// <param name="Data">The values the application reads when the notification is tapped.</param>
public sealed record PushMessage(string Title, string? Body, IReadOnlyDictionary<string, string> Data);

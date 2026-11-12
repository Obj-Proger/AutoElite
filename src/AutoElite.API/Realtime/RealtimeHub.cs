using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutoElite.API.Realtime;

/// <summary>What the connected clients of a user receive from <see cref="RealtimeHub"/>.</summary>
public interface IRealtimeClient
{
    /// <summary>A new notification of the user: the client updates the bell and reads the notification if it shows it.</summary>
    /// <param name="update">The notification.</param>
    Task NotificationReceived(NotificationUpdate update);

    /// <summary>A new message in a chat of the user: the client reads the new messages of the chat if it shows it.</summary>
    /// <param name="update">The message.</param>
    Task ChatMessageReceived(ChatMessageUpdate update);
}

/// <summary>
/// The SignalR hub through which signed-in users receive updates at once. A client connects with its access token,
/// which browsers send in the access_token parameter of the address, and only listens: everything it does goes
/// through the API.
/// </summary>
[Authorize]
public sealed class RealtimeHub : Hub<IRealtimeClient>;

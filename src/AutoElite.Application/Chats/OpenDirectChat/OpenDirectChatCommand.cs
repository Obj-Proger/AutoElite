namespace AutoElite.Application.Chats.OpenDirectChat;

/// <summary>
/// Returns the personal chat of the caller and another active user, creating it if they have none yet (FR-9.2), so
/// that files can be attached to it before the first message.
/// </summary>
/// <param name="UserId">The other user.</param>
public sealed record OpenDirectChatCommand(Guid UserId) : ICommand<Guid>;

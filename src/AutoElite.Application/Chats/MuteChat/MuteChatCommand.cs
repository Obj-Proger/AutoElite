namespace AutoElite.Application.Chats.MuteChat;

/// <summary>Turns the notifications of a chat off or back on for the signed-in user (FR-9.6).</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="IsMuted">Whether the notifications are turned off.</param>
public sealed record MuteChatCommand(Guid ChatId, bool IsMuted) : ICommand;

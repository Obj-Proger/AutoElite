namespace AutoElite.Application.Chats.LeaveChat;

/// <summary>
/// Leaves a group chat (FR-9.8). When the last administrator leaves, the longest-standing member becomes the
/// administrator.
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
public sealed record LeaveChatCommand(Guid ChatId) : ICommand;

namespace AutoElite.Application.Chats.MarkChatRead;

/// <summary>
/// Moves the read position of the signed-in user in a chat (FR-9.7). Messages sent up to the time count as read; the
/// position never moves back, and a time in the future counts as now.
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="ReadUpToUtc">The time of the latest message read, in UTC.</param>
public sealed record MarkChatReadCommand(Guid ChatId, DateTime ReadUpToUtc) : ICommand;

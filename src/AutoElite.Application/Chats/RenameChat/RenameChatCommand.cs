namespace AutoElite.Application.Chats.RenameChat;

/// <summary>Renames a group chat; only its administrators may do it (FR-9.8).</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="Title">The new title.</param>
public sealed record RenameChatCommand(Guid ChatId, string? Title) : ICommand;

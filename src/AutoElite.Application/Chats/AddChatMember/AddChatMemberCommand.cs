namespace AutoElite.Application.Chats.AddChatMember;

/// <summary>Adds an active user to a group chat; only its administrators may do it (FR-9.8).</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="UserId">The user to add.</param>
public sealed record AddChatMemberCommand(Guid ChatId, Guid UserId) : ICommand;

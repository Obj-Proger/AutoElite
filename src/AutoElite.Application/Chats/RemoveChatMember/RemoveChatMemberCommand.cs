namespace AutoElite.Application.Chats.RemoveChatMember;

/// <summary>
/// Removes a member from a group chat; only its administrators may do it (FR-9.8). The member keeps no access to
/// new messages.
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="UserId">The member to remove.</param>
public sealed record RemoveChatMemberCommand(Guid ChatId, Guid UserId) : ICommand;

namespace AutoElite.Application.Chats.ChangeChatMemberRole;

/// <summary>
/// Makes a member of a group chat an administrator or a plain member; only its administrators may do it, and the chat
/// keeps at least one administrator (FR-9.8).
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="UserId">The member.</param>
/// <param name="Role">The new role.</param>
public sealed record ChangeChatMemberRoleCommand(Guid ChatId, Guid UserId, ChatMemberRole Role) : ICommand;

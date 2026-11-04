namespace AutoElite.Application.Chats.GetChatMembers;

/// <summary>Reads the current members of a chat of the signed-in user with their roles and read positions.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
public sealed record GetChatMembersQuery(Guid ChatId) : IQuery<IReadOnlyList<ChatMemberItem>>;

namespace AutoElite.Application.Chats.GetMyChats;

/// <summary>Reads the chats of the signed-in user with the latest message and the number of unread ones (FR-9.7).</summary>
public sealed record GetMyChatsQuery : IQuery<IReadOnlyList<ChatListItem>>;

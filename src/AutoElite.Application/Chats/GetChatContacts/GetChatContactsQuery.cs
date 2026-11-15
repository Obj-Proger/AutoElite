namespace AutoElite.Application.Chats.GetChatContacts;

/// <summary>
/// Reads the people the signed-in user studies or teaches with, whom they can start a personal chat with (FR-9.2).
/// </summary>
public sealed record GetChatContactsQuery : IQuery<IReadOnlyList<ChatContactItem>>;

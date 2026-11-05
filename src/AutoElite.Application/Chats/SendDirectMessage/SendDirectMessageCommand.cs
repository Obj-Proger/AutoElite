namespace AutoElite.Application.Chats.SendDirectMessage;

/// <summary>
/// Sends a personal text message to another active user of the school. The first message to a new recipient creates
/// the personal chat of the pair, and later ones reuse it (FR-9.2). To send files, open the chat with
/// <see cref="OpenDirectChat.OpenDirectChatCommand"/>, attach them to it and use
/// <see cref="SendChatMessage.SendChatMessageCommand"/>.
/// </summary>
/// <param name="RecipientId">The user the message is for.</param>
/// <param name="Text">The text of the message.</param>
public sealed record SendDirectMessageCommand(Guid RecipientId, string? Text) : ICommand<Guid>;

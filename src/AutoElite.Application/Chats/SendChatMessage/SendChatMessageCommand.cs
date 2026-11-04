namespace AutoElite.Application.Chats.SendChatMessage;

/// <summary>Sends a message to a chat the signed-in user is a member of. Attachments come with the documents module.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="Text">The text of the message.</param>
public sealed record SendChatMessageCommand(Guid ChatId, string? Text) : ICommand<Guid>;

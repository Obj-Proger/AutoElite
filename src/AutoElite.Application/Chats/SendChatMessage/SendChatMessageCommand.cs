namespace AutoElite.Application.Chats.SendChatMessage;

/// <summary>
/// Sends a message to a chat the signed-in user is a member of. To send files, attach them to the chat as documents
/// first and give their identifiers here; each document is sent with one message.
/// </summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="Text">The text of the message; may be empty if there are attachments.</param>
/// <param name="AttachmentIds">The documents to attach, up to 10; none if not given.</param>
public sealed record SendChatMessageCommand(Guid ChatId, string? Text, IReadOnlyList<Guid>? AttachmentIds = null) : ICommand<Guid>;

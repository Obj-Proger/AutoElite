namespace AutoElite.Application.Chats.DeleteChatMessage;

/// <summary>Deletes a message of the signed-in user for all members of the chat (FR-9.5); the text and attachments are erased.</summary>
/// <param name="MessageId">The identifier of the message.</param>
public sealed record DeleteChatMessageCommand(Guid MessageId) : ICommand;

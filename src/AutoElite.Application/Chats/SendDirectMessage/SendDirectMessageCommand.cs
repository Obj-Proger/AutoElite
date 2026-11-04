namespace AutoElite.Application.Chats.SendDirectMessage;

/// <summary>
/// Sends a personal message to another active user of the school. The first message to a new recipient creates the
/// personal chat of the pair, and later ones reuse it (FR-9.2). Attachments come with the documents module.
/// </summary>
/// <param name="RecipientId">The user the message is for.</param>
/// <param name="Text">The text of the message.</param>
public sealed record SendDirectMessageCommand(Guid RecipientId, string? Text) : ICommand<Guid>;

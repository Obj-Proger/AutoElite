namespace AutoElite.Client.Realtime;

/// <summary>That a new message exists in a chat of the signed-in user, which the client reads from the API.</summary>
/// <param name="ChatId">The chat.</param>
/// <param name="MessageId">The message.</param>
/// <param name="SenderId">The user who sent it.</param>
public sealed record ChatMessageSignal(Guid ChatId, Guid MessageId, Guid SenderId);

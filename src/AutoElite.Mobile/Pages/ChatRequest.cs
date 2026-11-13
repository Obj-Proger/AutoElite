namespace AutoElite.Mobile.Pages;

/// <summary>The chat the chat page shows, passed to it when it is opened.</summary>
/// <param name="ChatId">The chat.</param>
/// <param name="Title">The title of the chat.</param>
public sealed record ChatRequest(Guid ChatId, string Title);

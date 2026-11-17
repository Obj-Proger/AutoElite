namespace AutoElite.Mobile.Pages;

/// <summary>The chat the chat page shows, passed to it when it is opened.</summary>
/// <param name="ChatId">The chat.</param>
/// <param name="Title">The title of the chat: the full name of the other person of a personal chat.</param>
/// <param name="IsPersonal">Whether the chat is a personal one, whose page is titled by the short name of the person.</param>
public sealed record ChatRequest(Guid ChatId, string Title, bool IsPersonal = false);

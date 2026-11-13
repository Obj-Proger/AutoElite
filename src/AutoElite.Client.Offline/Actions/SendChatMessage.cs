using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>A user sends a text message to a chat they are a member of.</summary>
/// <param name="ChatId">The chat.</param>
/// <param name="Text">The text.</param>
public sealed record SendChatMessage(Guid ChatId, string Text) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Api.V1.Chats[ChatId].Messages.PostAsync(
            new SendChatMessageCommand { ChatId = ChatId, Text = Text }, cancellationToken: cancellationToken);
    }
}

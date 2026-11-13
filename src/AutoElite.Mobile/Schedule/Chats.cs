using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The chats of the signed-in user and their latest messages, read from the API or, without a network, from the
/// device, and the read position of the user in a chat. Messages are sent through the offline queue.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class Chats(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    private const int Latest = 50;

    /// <summary>Reads the chats, the one with the latest message first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The chats.</returns>
    public Task<Cached<IReadOnlyList<ChatListItem>>> ReadChatsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "chats",
            token => createApi().Api.V1.Chats.GetAsync(cancellationToken: token),
            ChatListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the latest messages of a chat, the latest first.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The messages.</returns>
    public Task<Cached<IReadOnlyList<ChatMessageItem>>> ReadMessagesAsync(Guid chatId, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            string.Create(CultureInfo.InvariantCulture, $"chats/{chatId:N}/messages"),
            token => createApi().Api.V1.Chats[chatId].Messages.GetAsync(request => request.QueryParameters.Limit = Latest, token),
            ChatMessageItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Marks the messages of a chat read up to a time.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="readUpTo">The time of the latest message read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the position is moved.</returns>
    public Task MarkReadAsync(Guid chatId, DateTimeOffset readUpTo, CancellationToken cancellationToken) =>
        createApi().Api.V1.Chats[chatId].ReadPosition.PutAsync(
            new MarkChatReadCommand { ChatId = chatId, ReadUpToUtc = readUpTo }, cancellationToken: cancellationToken);
}

using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The chats of the signed-in user and their latest messages, read from the API or, without a network, from the
/// device, and the read position of the user in a chat; the people they can start a personal chat with; turning the
/// notifications of a chat off, leaving a group chat and deleting a message. Text messages are sent through the
/// offline queue; files need the network: each is attached to the chat first and then sent with one message. Files
/// are opened through <see cref="DocumentFiles"/>.
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

    /// <summary>Reads the people the user studies or teaches with, by name.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The people.</returns>
    public Task<Cached<IReadOnlyList<ChatContactItem>>> ReadContactsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "chats/contacts",
            token => createApi().Api.V1.Chats.Contacts.GetAsync(cancellationToken: token),
            ChatContactItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Gives the personal chat with another user, which the API creates if they have none yet.</summary>
    /// <param name="userId">The other user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The chat.</returns>
    public async Task<Guid> OpenDirectAsync(Guid userId, CancellationToken cancellationToken)
    {
        var chat = await createApi().Api.V1.Chats.Direct.PostAsync(
            new OpenDirectChatCommand { UserId = userId }, cancellationToken: cancellationToken);

        return chat?.ChatId ?? throw new InvalidOperationException("The API answered without the chat.");
    }

    /// <summary>Turns the notifications of a chat off or back on.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="isMuted">Whether the notifications are turned off.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the setting is saved.</returns>
    public Task SetMutedAsync(Guid chatId, bool isMuted, CancellationToken cancellationToken) =>
        createApi().Api.V1.Chats[chatId].Mute.PutAsync(
            new MuteChatCommand { ChatId = chatId, IsMuted = isMuted }, cancellationToken: cancellationToken);

    /// <summary>Leaves a group chat.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the user has left.</returns>
    public Task LeaveAsync(Guid chatId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Chats[chatId].Leave.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Deletes a message of the user for every member, with its files.</summary>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the message is deleted.</returns>
    public Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Chats.Messages[messageId].DeleteAsync(cancellationToken: cancellationToken);

    /// <summary>Reads the files of a chat, from the API or, without a network, from the device.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The documents of the files.</returns>
    public Task<Cached<IReadOnlyList<DocumentItem>>> ReadFilesAsync(Guid chatId, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            string.Create(CultureInfo.InvariantCulture, $"chats/{chatId:N}/files"),
            token => createApi().Api.V1.Documents.GetAsync(
                request =>
                {
                    request.QueryParameters.OwnerKind = EntityKind.Chat;
                    request.QueryParameters.OwnerId = chatId;
                },
                token),
            DocumentItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>
    /// Attaches a file to a chat, to be sent with a message; the API checks its size, up to 20 MB, and that its content
    /// matches its type.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="file">The file the user chose.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The document of the file.</returns>
    public async Task<Guid> AttachAsync(Guid chatId, FileResult file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        await using var content = await file.OpenReadAsync();
        var form = new MultipartBody();
        var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;
        form.AddOrReplacePart("file", contentType, content, file.FileName);
        form.AddOrReplacePart("ownerKind", "text/plain", nameof(EntityKind.Chat));
        form.AddOrReplacePart("ownerId", "text/plain", chatId.ToString("D", CultureInfo.InvariantCulture));
        var document = await createApi().AttachDocumentAsync(form, cancellationToken);

        return document.Id ?? throw new InvalidOperationException("The API answered without the document.");
    }

    /// <summary>Deletes a file attached to a chat that the user decided not to send.</summary>
    /// <param name="documentId">The document of the file.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the file is deleted.</returns>
    public Task DeleteFileAsync(Guid documentId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Documents[documentId].DeleteAsync(cancellationToken: cancellationToken);

    /// <summary>Sends a message with files attached to the chat beforehand; it needs the network.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="text">The text, which may be empty.</param>
    /// <param name="documentIds">The documents of the files, up to 10.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the message is sent.</returns>
    public Task SendWithFilesAsync(Guid chatId, string text, IReadOnlyList<Guid> documentIds, CancellationToken cancellationToken) =>
        createApi().Api.V1.Chats[chatId].Messages.PostAsync(
            new SendChatMessageCommand
            {
                ChatId = chatId,
                Text = string.IsNullOrWhiteSpace(text) ? null : text,
                AttachmentIds = [.. documentIds.Select(id => (Guid?)id)]
            },
            cancellationToken: cancellationToken);
}

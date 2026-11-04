namespace AutoElite.Application.Chats.GetChatMessages;

/// <summary>Hides chats the caller is not a member of as not found and reads the batch.</summary>
internal sealed class GetChatMessagesQueryHandler(IChatQueries chatQueries, ICurrentUser currentUser)
    : IQueryHandler<GetChatMessagesQuery, IReadOnlyList<ChatMessageItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatMessageItem>>> Handle(GetChatMessagesQuery query, CancellationToken cancellationToken)
    {
        if (!await chatQueries.IsCurrentMemberAsync(query.ChatId, currentUser.GetRequiredUserId(), cancellationToken))
            return Result.Failure<IReadOnlyList<ChatMessageItem>>(ApplicationErrors.NotFound<Chat>());

        return Result.Success(await chatQueries.GetMessagesAsync(query.ChatId, query.BeforeMessageId, query.Limit, cancellationToken));
    }
}

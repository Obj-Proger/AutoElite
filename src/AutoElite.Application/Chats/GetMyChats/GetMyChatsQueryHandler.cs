namespace AutoElite.Application.Chats.GetMyChats;

/// <summary>Reads the caller's chats.</summary>
internal sealed class GetMyChatsQueryHandler(IChatQueries chatQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyChatsQuery, IReadOnlyList<ChatListItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatListItem>>> Handle(GetMyChatsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await chatQueries.GetOfUserAsync(currentUser.GetRequiredUserId(), cancellationToken));
}

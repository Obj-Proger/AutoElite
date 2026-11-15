namespace AutoElite.Application.Chats.GetChatContacts;

/// <summary>Reads the caller's contacts.</summary>
internal sealed class GetChatContactsQueryHandler(IChatQueries chatQueries, ICurrentUser currentUser)
    : IQueryHandler<GetChatContactsQuery, IReadOnlyList<ChatContactItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatContactItem>>> Handle(GetChatContactsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await chatQueries.GetContactsAsync(currentUser.GetRequiredUserId(), cancellationToken));
}

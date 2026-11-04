namespace AutoElite.Application.Chats.GetChatMembers;

/// <summary>Hides chats the caller is not a member of as not found and reads the members.</summary>
internal sealed class GetChatMembersQueryHandler(IChatQueries chatQueries, ICurrentUser currentUser)
    : IQueryHandler<GetChatMembersQuery, IReadOnlyList<ChatMemberItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatMemberItem>>> Handle(GetChatMembersQuery query, CancellationToken cancellationToken)
    {
        if (!await chatQueries.IsCurrentMemberAsync(query.ChatId, currentUser.GetRequiredUserId(), cancellationToken))
            return Result.Failure<IReadOnlyList<ChatMemberItem>>(ApplicationErrors.NotFound<Chat>());

        return Result.Success(await chatQueries.GetMembersAsync(query.ChatId, cancellationToken));
    }
}

namespace AutoElite.Application.Chats;

/// <summary>Loads a chat for an action of one of its current members; for anyone else the chat is reported as not found.</summary>
internal sealed class ChatAccess(IChatRepository chatRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the chat if the caller is a current member.</summary>
    /// <param name="chatId">The identifier of the chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Chat>> LoadAsMemberAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var chat = await chatRepository.GetByIdAsync(chatId, cancellationToken);

        return chat is not null && chat.IsCurrentMember(currentUser.GetRequiredUserId())
            ? chat
            : Result.Failure<Chat>(ApplicationErrors.NotFound<Chat>());
    }
}

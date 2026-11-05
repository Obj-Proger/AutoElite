using AutoElite.Application.Users;

namespace AutoElite.Application.Chats;

/// <summary>Finds the personal chat of the caller and another user, creating it on first use (FR-9.2).</summary>
internal sealed class DirectChats(IChatRepository chatRepository, IUserRepository userRepository, ICurrentUser currentUser)
{
    /// <summary>Finds or creates the chat. The other user must be active.</summary>
    /// <param name="otherUserId">The other user.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Chat>> GetOrCreateAsync(Guid otherUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var callerId = currentUser.GetRequiredUserId();
        var other = await userRepository.GetByIdAsync(otherUserId, cancellationToken);
        if (other is not { IsActive: true })
            return Result.Failure<Chat>(ApplicationErrors.NotFound<User>());

        var chat = await chatRepository.GetDirectAsync(Chat.BuildDirectKey(callerId, other.Id), cancellationToken);
        if (chat is not null)
            return chat;

        var chatResult = Chat.CreateDirect(callerId, other.Id, utcNow);
        if (chatResult.IsSuccess)
            chatRepository.Add(chatResult.Value);

        return chatResult;
    }
}

using AutoElite.Application.Users;

namespace AutoElite.Application.Chats;

/// <summary>Checks the users who are added to chats.</summary>
internal sealed class ChatUsers(IUserRepository userRepository)
{
    /// <summary>Checks that each user exists and is active; a deactivated user cannot join a chat.</summary>
    /// <param name="userIds">The users to check.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckActiveAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds.Distinct())
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is not { IsActive: true })
                return Result.Failure(ApplicationErrors.NotFound<User>());
        }

        return Result.Success();
    }
}

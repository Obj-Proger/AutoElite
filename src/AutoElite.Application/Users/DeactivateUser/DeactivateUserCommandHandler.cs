using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.DeactivateUser;

/// <summary>Deactivates the user and revokes all of the user's refresh tokens.</summary>
internal sealed class DeactivateUserCommandHandler(
    ManagedUsers managedUsers,
    IUserRoleRepository userRoleRepository,
    IRefreshTokenStore refreshTokenStore,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeactivateUserCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == currentUser.GetRequiredUserId())
            return Result.Failure(ApplicationErrors.Account.CannotDeactivateSelf);

        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);
        if (managedResult.IsFailure)
            return Result.Failure(managedResult.Error);

        var (user, roles) = managedResult.Value;
        if (user.IsActive
            && roles.Contains(UserRole.Administrator)
            && await userRoleRepository.CountActiveAdministratorsAsync(cancellationToken) <= 1)
        {
            return Result.Failure(DomainErrors.UserRoleAssignment.LastAdministrator);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var deactivationResult = user.Deactivate(utcNow);
        if (deactivationResult.IsFailure)
            return deactivationResult;

        await refreshTokenStore.RevokeAllAsync(user.Id, utcNow, cancellationToken);

        return Result.Success();
    }
}

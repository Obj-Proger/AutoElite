namespace AutoElite.Application.Users.ReactivateUser;

/// <summary>Reactivates the user.</summary>
internal sealed class ReactivateUserCommandHandler(ManagedUsers managedUsers) : ICommandHandler<ReactivateUserCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);

        return managedResult.IsFailure ? Result.Failure(managedResult.Error) : managedResult.Value.User.Reactivate();
    }
}

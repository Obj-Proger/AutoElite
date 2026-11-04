namespace AutoElite.Application.Users.ChangeUserBranch;

/// <summary>Checks the branch and assigns the user to it.</summary>
internal sealed class ChangeUserBranchCommandHandler(
    ManagedUsers managedUsers,
    IRepository<Branch> branchRepository,
    ICurrentUser currentUser) : ICommandHandler<ChangeUserBranchCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeUserBranchCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);
        if (managedResult.IsFailure)
            return Result.Failure(managedResult.Error);

        var branchResult = await UserBranches.CheckAsync(
            branchRepository, command.BranchId, DataScope.For(currentUser), cancellationToken);
        if (branchResult.IsFailure)
            return branchResult;

        return managedResult.Value.User.AssignBranch(command.BranchId);
    }
}

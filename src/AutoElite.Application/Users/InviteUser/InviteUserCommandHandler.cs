namespace AutoElite.Application.Users.InviteUser;

/// <summary>Checks what the caller may invite and into which branch, then registers the user.</summary>
internal sealed class InviteUserCommandHandler(
    UserRegistration userRegistration,
    IRepository<Branch> branchRepository,
    ICurrentUser currentUser) : ICommandHandler<InviteUserCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var roles = command.Roles ?? [];
        if (!currentUser.IsInRole(UserRole.Administrator) && roles.Any(role => role != UserRole.Student))
            return Result.Failure<Guid>(ApplicationErrors.Account.ManagerInvitesOnlyStudents);

        var scope = DataScope.For(currentUser);
        var branchId = command.BranchId ?? scope.BranchId;
        var branchResult = await BranchChecks.CheckAsync(branchRepository, branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var details = new NewUserDetails(
            command.LastName, command.FirstName, command.MiddleName, command.Email, command.Phone, branchId, command.PreferredLanguage);
        var userResult = await userRegistration.RegisterAsync(details, roles, cancellationToken);

        return userResult.IsFailure ? Result.Failure<Guid>(userResult.Error) : userResult.Value.Id;
    }
}

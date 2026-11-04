using AutoElite.Domain.Policies;

namespace AutoElite.Application.Users.InviteUser;

/// <summary>
/// Creates the user, the role assignments and the sign-in account. The invitation email is sent after the commit
/// by <see cref="Invitations.SendInvitationWhenUserRegisteredHandler"/>.
/// </summary>
internal sealed class InviteUserCommandHandler(
    IUserRepository userRepository,
    IRepository<UserRoleAssignment> roleAssignmentRepository,
    IRepository<Branch> branchRepository,
    IIdentityService identityService,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<InviteUserCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var roles = command.Roles ?? [];
        if (!currentUser.IsInRole(UserRole.Administrator) && roles.Any(role => role != UserRole.Student))
            return Result.Failure<Guid>(ApplicationErrors.Account.ManagerInvitesOnlyStudents);

        var scope = DataScope.For(currentUser);
        var branchId = command.BranchId ?? scope.BranchId;
        var branchResult = await CheckBranchAsync(branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var userResult = await CreateUserAsync(command, branchId, cancellationToken);
        if (userResult.IsFailure)
            return Result.Failure<Guid>(userResult.Error);

        var user = userResult.Value;
        var assignmentsResult = CreateRoleAssignments(user, roles);
        if (assignmentsResult.IsFailure)
            return Result.Failure<Guid>(assignmentsResult.Error);

        userRepository.Add(user);
        foreach (var assignment in assignmentsResult.Value)
            roleAssignmentRepository.Add(assignment);

        await identityService.CreateAccountAsync(user.Id, user.Email, cancellationToken);

        return user.Id;
    }

    private async Task<Result> CheckBranchAsync(Guid? branchId, DataScope scope, CancellationToken cancellationToken)
    {
        if (branchId is not { } id)
            return Result.Success();

        var branch = scope.Includes(id) ? await branchRepository.GetByIdAsync(id, cancellationToken) : null;
        if (branch is null)
            return Result.Failure(ApplicationErrors.NotFound<Branch>());

        return branch.IsActive ? Result.Success() : Result.Failure(ApplicationErrors.Account.BranchInactive);
    }

    private async Task<Result<User>> CreateUserAsync(InviteUserCommand command, Guid? branchId, CancellationToken cancellationToken)
    {
        var fullNameResult = FullName.Create(command.LastName, command.FirstName, command.MiddleName);
        if (fullNameResult.IsFailure)
            return Result.Failure<User>(fullNameResult.Error);

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Failure<User>(emailResult.Error);

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            var phoneResult = PhoneNumber.Create(command.Phone);
            if (phoneResult.IsFailure)
                return Result.Failure<User>(phoneResult.Error);

            phone = phoneResult.Value;
        }

        if (await userRepository.EmailExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<User>(ApplicationErrors.Account.EmailTaken);

        return User.Create(
            fullNameResult.Value,
            emailResult.Value,
            phone,
            branchId,
            timeProvider.GetUtcNow().UtcDateTime,
            command.PreferredLanguage);
    }

    private static Result<List<UserRoleAssignment>> CreateRoleAssignments(User user, IReadOnlyList<UserRole> roles)
    {
        var granted = new List<UserRole>();
        var assignments = new List<UserRoleAssignment>();

        foreach (var role in roles)
        {
            var policyResult = RoleAssignmentPolicy.CanAssign(user, granted, role);
            if (policyResult.IsFailure)
                return Result.Failure<List<UserRoleAssignment>>(policyResult.Error);

            var assignmentResult = UserRoleAssignment.Create(user.Id, role);
            if (assignmentResult.IsFailure)
                return Result.Failure<List<UserRoleAssignment>>(assignmentResult.Error);

            granted.Add(role);
            assignments.Add(assignmentResult.Value);
        }

        return assignments;
    }
}

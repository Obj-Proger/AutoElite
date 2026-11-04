using AutoElite.Domain.Policies;

namespace AutoElite.Application.Users;

/// <summary>
/// Registers a new user with roles and a sign-in account without a password. The invitation email is sent after the
/// commit by <see cref="Invitations.SendInvitationWhenUserRegisteredHandler"/>.
/// </summary>
internal sealed class UserRegistration(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IIdentityService identityService,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Checks the details, the unique email and the role compatibility policy, then adds the user, the role
    /// assignments and the account. Nothing is added if a check fails.
    /// </summary>
    /// <param name="details">The details of the user.</param>
    /// <param name="roles">The roles to grant, at least one.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<User>> RegisterAsync(
        NewUserDetails details, IReadOnlyList<UserRole> roles, CancellationToken cancellationToken)
    {
        var userResult = await CreateUserAsync(details, cancellationToken);
        if (userResult.IsFailure)
            return userResult;

        var user = userResult.Value;
        var assignmentsResult = CreateRoleAssignments(user, roles);
        if (assignmentsResult.IsFailure)
            return Result.Failure<User>(assignmentsResult.Error);

        userRepository.Add(user);
        foreach (var assignment in assignmentsResult.Value)
            userRoleRepository.Add(assignment);

        await identityService.CreateAccountAsync(user.Id, user.Email, cancellationToken);

        return user;
    }

    private async Task<Result<User>> CreateUserAsync(NewUserDetails details, CancellationToken cancellationToken)
    {
        var fullNameResult = FullName.Create(details.LastName, details.FirstName, details.MiddleName);
        if (fullNameResult.IsFailure)
            return Result.Failure<User>(fullNameResult.Error);

        var emailResult = Email.Create(details.Email);
        if (emailResult.IsFailure)
            return Result.Failure<User>(emailResult.Error);

        var phoneResult = ContactDetails.CreateOptionalPhone(details.Phone);
        if (phoneResult.IsFailure)
            return Result.Failure<User>(phoneResult.Error);

        if (await userRepository.EmailExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<User>(ApplicationErrors.Account.EmailTaken);

        return User.Create(
            fullNameResult.Value,
            emailResult.Value,
            phoneResult.Value,
            details.BranchId,
            timeProvider.GetUtcNow().UtcDateTime,
            details.PreferredLanguage);
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

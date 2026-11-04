using AutoElite.Application.Users;

namespace AutoElite.Application.Contracts;

/// <summary>Loads an existing student for a new contract.</summary>
internal sealed class ContractStudents(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    ICurrentUser currentUser)
{
    /// <summary>
    /// Loads the user if it is inside the caller's scope and holds the student role. The contract checks that the
    /// student is active.
    /// </summary>
    /// <param name="studentId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<User>> LoadAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null || !DataScope.For(currentUser).Includes(student.BranchId))
            return Result.Failure<User>(ApplicationErrors.NotFound<User>());

        var roles = await userRoleRepository.GetRolesAsync(student.Id, cancellationToken);

        return roles.Contains(UserRole.Student) ? student : Result.Failure<User>(ApplicationErrors.Contract.NotAStudent);
    }
}

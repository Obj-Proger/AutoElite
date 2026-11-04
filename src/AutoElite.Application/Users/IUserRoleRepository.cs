namespace AutoElite.Application.Users;

/// <summary>Loads and adds role assignments for commands.</summary>
public interface IUserRoleRepository : IRepository<UserRoleAssignment>
{
    /// <summary>Gets the roles the user holds.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<UserRole>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Loads the assignment of one role to the user.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="role">The role.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The assignment, or <see langword="null"/> if the user does not hold the role.</returns>
    Task<UserRoleAssignment?> GetAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the active users who hold the administrator role, and locks those assignments until the transaction ends,
    /// so that two requests cannot both take away the last administrator.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes an assignment; it is deleted on the next save.</summary>
    /// <param name="assignment">The assignment to remove.</param>
    void Remove(UserRoleAssignment assignment);
}

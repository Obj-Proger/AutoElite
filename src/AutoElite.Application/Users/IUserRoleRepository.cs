namespace AutoElite.Application.Users;

/// <summary>Loads and adds role assignments for commands.</summary>
public interface IUserRoleRepository : IRepository<UserRoleAssignment>
{
    /// <summary>Gets the roles the user holds.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<UserRole>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}

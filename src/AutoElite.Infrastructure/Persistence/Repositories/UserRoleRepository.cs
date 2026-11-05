using AutoElite.Application.Users;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads, adds and removes role assignments.</summary>
internal sealed class UserRoleRepository(AutoEliteDbContext dbContext)
    : Repository<UserRoleAssignment>(dbContext), IUserRoleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRole>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Aggregates.Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.Role)
            .OrderBy(role => role)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<UserRoleAssignment?> GetAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default) =>
        Aggregates.FirstOrDefaultAsync(assignment => assignment.UserId == userId && assignment.Role == role, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Both the assignments and their users are locked (<c>FOR UPDATE</c>) in the transaction of the request, which this
    /// opens if needed: a concurrent request that revokes the role or deactivates one of the users waits until this one
    /// is saved, and then counts again with that change.
    /// </remarks>
    public async Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        await DbContext.BeginIfNoneAsync(cancellationToken);

        var administrator = nameof(UserRole.Administrator);
        var locked = await DbContext.Database
            .SqlQuery<Guid>($"""
                SELECT a.id AS "Value"
                FROM user_role_assignments AS a
                JOIN users AS u ON u.id = a.user_id
                WHERE a.role = {administrator} AND u.is_active
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return locked.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveHoldersAsync(
        UserRole role, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var holders = DbContext.Set<User>()
            .Where(user => user.IsActive && Aggregates.Any(assignment => assignment.UserId == user.Id && assignment.Role == role));
        if (branchId is { } branch)
            holders = holders.Where(user => user.BranchId == null || user.BranchId == branch);

        return await holders.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(UserRoleAssignment assignment) => Aggregates.Remove(assignment);
}

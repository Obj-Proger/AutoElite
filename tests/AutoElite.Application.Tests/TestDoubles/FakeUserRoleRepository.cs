using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

/// <param name="users">
/// The users whose activity decides which administrators count, all count if not given; also the source of
/// <see cref="GetActiveHoldersAsync"/>.
/// </param>
internal sealed class FakeUserRoleRepository(FakeUserRepository? users = null)
    : InMemoryRepository<UserRoleAssignment>, IUserRoleRepository
{
    public Task<IReadOnlyList<UserRole>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserRole>>([.. Items.Where(item => item.UserId == userId).Select(item => item.Role)]);

    public Task<UserRoleAssignment?> GetAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId && item.Role == role));

    public Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(item => item.Role == UserRole.Administrator
            && (users is null || users.Items.Any(user => user.Id == item.UserId && user.IsActive))));

    public Task<IReadOnlyList<User>> GetActiveHoldersAsync(
        UserRole role, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var holderIds = Items.Where(item => item.Role == role).Select(item => item.UserId).ToHashSet();

        return Task.FromResult<IReadOnlyList<User>>(
        [
            .. (users?.Items ?? []).Where(user => user.IsActive && holderIds.Contains(user.Id)
                && (branchId is null || user.BranchId is null || user.BranchId == branchId))
        ]);
    }

    /// <summary>Grants a role that the user holds before the test acts.</summary>
    public void Seed(Guid userId, UserRole role) => Seed(UserRoleAssignment.Create(userId, role).Value);
}

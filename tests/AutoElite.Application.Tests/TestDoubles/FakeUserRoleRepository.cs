using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeUserRoleRepository : InMemoryRepository<UserRoleAssignment>, IUserRoleRepository
{
    public Task<IReadOnlyList<UserRole>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserRole>>([.. Items.Where(item => item.UserId == userId).Select(item => item.Role)]);

    /// <summary>Grants a role that the user holds before the test acts.</summary>
    public void Seed(Guid userId, UserRole role) => Seed(UserRoleAssignment.Create(userId, role).Value);
}

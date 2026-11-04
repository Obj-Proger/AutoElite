using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

/// <summary>Users, roles and callers shared by the tests of the user management commands.</summary>
internal sealed class UserManagementFixture
{
    public UserManagementFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
    }

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public FakeRefreshTokenStore RefreshTokens { get; } = new();

    public InMemoryRepository<Branch> Branches { get; } = new();

    public User SeedUser(string email, Guid? branchId = null, params UserRole[] roles)
    {
        var user = TestData.NewUser(email, branchId);
        Users.Seed(user);
        foreach (var role in roles)
            Roles.Seed(user.Id, role);

        return user;
    }

    public FakeCurrentUser SeedAdministrator()
    {
        var administrator = SeedUser($"admin-{Users.Items.Count}@example.com", null, UserRole.Administrator);

        return new FakeCurrentUser { UserId = administrator.Id, Roles = new HashSet<UserRole> { UserRole.Administrator } };
    }

    public ManagedUsers CreateManagedUsers(FakeCurrentUser caller) => new(Users, Roles, caller);
}

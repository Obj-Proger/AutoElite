using AutoElite.Application.Contracts;
using AutoElite.Application.Leads;
using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Contracts;

/// <summary>Students, courses, branches, leads and contracts shared by the contract command tests.</summary>
internal sealed class ContractFixture
{
    public static readonly DateOnly Start = new(2026, 3, 10);
    public static readonly DateOnly End = new(2026, 7, 31);

    public ContractFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
        Courses.Seed(Course);
    }

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public FakeIdentityService Identity { get; } = new();

    public InMemoryRepository<Course> Courses { get; } = new();

    public InMemoryRepository<Branch> Branches { get; } = new();

    public InMemoryRepository<Lead> Leads { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public Course Course { get; } = TestData.NewCourse();

    public ContractStudents CreateContractStudents(FakeCurrentUser caller) => new(Users, Roles, caller);

    public ContractFactory CreateContractFactory() =>
        new(Contracts, new FakeContractNumberGenerator(), Courses, TestData.Clock());

    public UserRegistration CreateUserRegistration() => new(Users, Roles, Identity, TestData.Clock());

    public LeadAccess CreateLeadAccess(FakeCurrentUser caller) => new(Leads, caller);

    public Branch SeedBranch(string name = "Central")
    {
        var branch = TestData.NewBranch(name);
        Branches.Seed(branch);

        return branch;
    }

    public User SeedStudent(string email = "ivan@example.com", Guid? branchId = null, UserRole role = UserRole.Student)
    {
        var user = TestData.NewUser(email, branchId);
        Users.Seed(user);
        Roles.Seed(user.Id, role);

        return user;
    }
}

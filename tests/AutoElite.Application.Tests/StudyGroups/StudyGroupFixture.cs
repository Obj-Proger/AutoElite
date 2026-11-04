using AutoElite.Application.Contracts;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StudyGroups;

/// <summary>Groups, courses, contracts and staff shared by the study group tests.</summary>
internal sealed class StudyGroupFixture
{
    public static readonly DateOnly Start = new(2026, 3, 10);
    public static readonly DateOnly End = new(2026, 7, 31);

    public StudyGroupFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
        Courses.Seed(Course);
    }

    public FakeStudyGroupRepository Groups { get; } = new();

    public InMemoryRepository<Course> Courses { get; } = new();

    public InMemoryRepository<Branch> Branches { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public Course Course { get; } = TestData.NewCourse();

    public StudyGroupAccess CreateGroupAccess(FakeCurrentUser caller) => new(Groups, caller);

    public ContractAccess CreateContractAccess(FakeCurrentUser caller) => new(Contracts, caller);

    public GroupInstructors CreateGroupInstructors() => new(new StaffMembers(Users, Roles));

    public Branch SeedBranch(string name = "Central")
    {
        var branch = TestData.NewBranch(name);
        Branches.Seed(branch);

        return branch;
    }

    public User SeedInstructor(Guid? branchId = null, string email = "teacher@example.com", UserRole role = UserRole.TheoryInstructor)
    {
        var user = TestData.NewUser(email, branchId);
        Users.Seed(user);
        Roles.Seed(user.Id, role);

        return user;
    }

    public StudyGroup SeedGroup(Guid? branchId = null, int capacity = 20)
    {
        var instructor = SeedInstructor(branchId, $"teacher-{Groups.Items.Count}@example.com");
        var group = StudyGroup.Create("B-2026-03", Course, instructor.Id, branchId, Start, End, capacity).Value;
        Groups.Seed(group);

        return group;
    }

    public Contract SeedContract(Guid? branchId = null, Course? course = null)
    {
        var student = TestData.NewUser($"student-{Contracts.Items.Count}@example.com", branchId);
        var contractCourse = course ?? Course;
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", student, contractCourse, contractCourse.Price, Transmission.Manual, branchId, Start, End,
                TestData.Now)
            .Value;
        Contracts.Seed(contract);

        return contract;
    }
}

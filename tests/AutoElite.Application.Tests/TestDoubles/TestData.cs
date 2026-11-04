using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Ready-made domain objects for handler tests.</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    public static FixedTimeProvider Clock() => new(new DateTimeOffset(Now));

    public static User NewUser(
        string email = "ivan@example.com",
        Guid? branchId = null,
        Language language = Language.Russian,
        string firstName = "Ivan") =>
        User.Create(FullName.Create("Ivanov", firstName).Value, Email.Create(email).Value, null, branchId, Now, language).Value;

    public static Branch NewBranch(string name = "Central") =>
        Branch.Create(name, Address.Create("Yekaterinburg", "Lenina 1").Value, null, "Asia/Yekaterinburg").Value;

    public static FakeCurrentUser Administrator() => FakeCurrentUser.SignedIn(UserRole.Administrator);

    public static FakeCurrentUser Manager(Guid? branchId) => new()
    {
        UserId = Guid.CreateVersion7(),
        BranchId = branchId,
        Roles = new HashSet<UserRole> { UserRole.Manager }
    };
}

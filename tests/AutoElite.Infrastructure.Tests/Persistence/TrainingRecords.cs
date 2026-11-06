using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Tests.Persistence;

/// <summary>Builds the records that contracts and groups refer to, each with names of its own.</summary>
internal static class TrainingRecords
{
    public static readonly Currency Rubles = Currency.Create("RUB").Value;

    public static User Person(string lastName, string firstName = "Иван", string? middleName = "Петрович") =>
        User.Create(
                FullName.Create(lastName, firstName, middleName).Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value,
                null, null, FixedClock.Default.UtcNow)
            .Value;

    public static Course NewCourse(string name = "Категория B") =>
        Course.Create(
                $"{name} {Guid.NewGuid():N}", LicenseCategory.B, Money.Create(60_000m, Rubles).Value, 134, 56, null)
            .Value;

    public static Branch NewBranch() =>
        Branch.Create(
                $"Branch {Guid.NewGuid():N}", Address.Create("Екатеринбург", "ул. Ленина, 1").Value, null, "Asia/Yekaterinburg")
            .Value;

    public static Contract NewContract(
        string number, User student, Course course, Guid? branchId = null, DateOnly? startDate = null) =>
        Contract.Create(
                number, student, course, course.Price, Transmission.Manual, branchId,
                startDate ?? new DateOnly(2026, 2, 1), (startDate ?? new DateOnly(2026, 2, 1)).AddMonths(4),
                FixedClock.Default.UtcNow)
            .Value;

    /// <summary>Gives a unique contract number.</summary>
    public static StudyGroup NewGroup(Course course, User instructor, Guid? branchId = null) =>
        StudyGroup.Create(
                $"Группа {Guid.NewGuid():N}", course, instructor.Id, branchId, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 1), 20)
            .Value;

    public static string Number() => "T-" + Guid.NewGuid().ToString("N")[..20];
}

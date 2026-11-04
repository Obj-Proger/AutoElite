using AutoElite.Application.Notifications;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Notifications;

/// <summary>Users, branches and the notification store shared by the tests of the event notifications.</summary>
internal sealed class NotificationFixture
{
    public NotificationFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
        Branches.Seed(Branch);
    }

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public FakeNotificationRepository Notifications { get; } = new();

    public FakeBranchRepository Branches { get; } = new();

    public FakeSchoolSettingsRepository Settings { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    /// <summary>Gets a branch in Asia/Yekaterinburg, five hours ahead of UTC.</summary>
    public Branch Branch { get; } = TestData.NewBranch();

    public LocalTimes CreateLocalTimes() => new(Branches, Settings);

    public Notifier CreateNotifier() => new(Users, Notifications);

    public User SeedUser(string email, Guid? branchId = null, Language language = Language.Russian, string firstName = "Ivan")
    {
        var user = TestData.NewUser(email, branchId, language, firstName);
        Users.Seed(user);

        return user;
    }

    public User SeedManager(string email, Guid? branchId, Language language = Language.Russian)
    {
        var manager = SeedUser(email, branchId, language);
        Roles.Seed(manager.Id, UserRole.Manager);

        return manager;
    }

    public Contract SeedContract(User student, Guid? branchId, Course? course = null)
    {
        course ??= TestData.NewCourse();
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", student, course, course.Price, Transmission.Manual, branchId,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        Contracts.Seed(contract);

        return contract;
    }

    /// <summary>Gets the notifications of a recipient.</summary>
    public IReadOnlyList<Notification> Of(Guid recipientId) =>
        [.. Notifications.Items.Where(notification => notification.RecipientId == recipientId)];
}

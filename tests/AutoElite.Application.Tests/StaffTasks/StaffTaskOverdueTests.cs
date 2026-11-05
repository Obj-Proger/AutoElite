using AutoElite.Application.Notifications.StaffTasks;
using AutoElite.Application.StaffTasks.RemindOverdueStaffTasks;
using AutoElite.Application.Tests.Notifications;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.StaffTasks;

public class StaffTaskOverdueTests
{
    /// <summary>18:00 in the branch's time zone, five hours ahead of UTC.</summary>
    private static readonly DateTime _due = new(2026, 3, 2, 13, 0, 0, DateTimeKind.Utc);

    private readonly NotificationFixture _fixture = new();
    private readonly FakeStaffTaskRepository _tasks = new();
    private readonly User _assignee;
    private readonly User _manager;

    public StaffTaskOverdueTests()
    {
        _assignee = _fixture.SeedUser("instructor@example.com", _fixture.Branch.Id);
        _manager = _fixture.SeedManager("manager@example.com", _fixture.Branch.Id);
    }

    private StaffTask SeedTask(Guid? createdByUserId)
    {
        var task = StaffTask.Create("Check the car", null, _assignee.Id, _due, null, TaskRecurrence.None, createdByUserId, TestData.Now)
            .Value;
        _tasks.Seed(task);

        return task;
    }

    private Task<Result> Remind(DateTime utcNow) =>
        new RemindOverdueStaffTasksCommandHandler(_tasks, new FixedTimeProvider(new DateTimeOffset(utcNow)))
            .Handle(new RemindOverdueStaffTasksCommand(), TestContext.Current.CancellationToken);

    private StaffTaskNotices CreateNotices() => new(_fixture.Users, _fixture.CreateLocalTimes());

    private Task Assigned(StaffTask task) =>
        new NotifyWhenStaffTaskAssignedHandler(_tasks, CreateNotices(), _fixture.CreateNotifier())
            .Handle(task.DomainEvents.OfType<StaffTaskAssignedDomainEvent>().Last(), TestContext.Current.CancellationToken);

    private Task Overdue(StaffTaskOverdueDomainEvent domainEvent) =>
        new NotifyWhenStaffTaskOverdueHandler(_tasks, CreateNotices(), _fixture.CreateNotifier())
            .Handle(domainEvent, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Remind_OverdueOpenTask_MarksItOnce()
    {
        var task = SeedTask(_manager.Id);

        (await Remind(_due.AddMinutes(5))).IsSuccess.Should().BeTrue();
        (await Remind(_due.AddMinutes(10))).IsSuccess.Should().BeTrue();

        task.OverdueNotifiedAtUtc.Should().Be(_due.AddMinutes(5));
        task.DomainEvents.OfType<StaffTaskOverdueDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Remind_TaskNotYetDue_LeavesIt()
    {
        var task = SeedTask(_manager.Id);

        var result = await Remind(_due.AddMinutes(-5));

        result.IsSuccess.Should().BeTrue();

        task.OverdueNotifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Assigned_BySomeoneElse_NotifiesTheAssigneeWithTheLocalDeadline()
    {
        var task = SeedTask(_manager.Id);

        await Assigned(task);

        var notification = _fixture.Of(_assignee.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.TaskAssigned);
        notification.Title.Should().Be("Новая задача");
        notification.Body.Should().Be("«Check the car». Срок — 2 марта в 18:00.");
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.StaffTask, task.Id).Value);
    }

    [Fact]
    public async Task Assigned_ByTheAssigneeThemselves_NotifiesNobody()
    {
        await Assigned(SeedTask(_assignee.Id));

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Overdue_NotifiesTheAssignee()
    {
        var task = SeedTask(_manager.Id);
        task.MarkOverdue(_due.AddMinutes(5)).IsSuccess.Should().BeTrue();

        await Overdue(task.DomainEvents.OfType<StaffTaskOverdueDomainEvent>().Single());

        var notification = _fixture.Of(_assignee.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.TaskOverdue);
        notification.Body.Should().Be("Срок задачи «Check the car» истёк 2 марта в 18:00.");
    }

    [Fact]
    public async Task Overdue_DeadlineMovedBeforeTheEventIsHandled_NotifiesNobody()
    {
        var task = SeedTask(_manager.Id);
        task.MarkOverdue(_due.AddMinutes(5)).IsSuccess.Should().BeTrue();
        var overdue = task.DomainEvents.OfType<StaffTaskOverdueDomainEvent>().Single();
        task.UpdateDetails(task.Title, null, _due.AddDays(1), null, _due.AddMinutes(10)).IsSuccess.Should().BeTrue();

        await Overdue(overdue);

        _fixture.Notifications.Items.Should().BeEmpty();
    }
}

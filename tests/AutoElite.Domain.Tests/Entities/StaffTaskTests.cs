namespace AutoElite.Domain.Tests.Entities;

public class StaffTaskTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _due = new(2026, 4, 3, 18, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _managerId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _otherManagerId = new("22222222-2222-2222-2222-222222222222");
    private static readonly EntityReference _lead = EntityReference.Create(EntityKind.Lead, new("55555555-5555-5555-5555-555555555555")).Value;

    private static StaffTask CreateTask(TaskRecurrence recurrence = TaskRecurrence.None) =>
        StaffTask.Create("Call the lead back", null, _managerId, _due, _lead, recurrence, null, _now).Value;

    [Fact]
    public void Create_ValidData_ReturnsNewTaskAndRaisesAssignedEvent()
    {
        var result = StaffTask.Create(
            "  Call the lead back  ", "  Ask about evening groups  ", _managerId, _due, _lead, TaskRecurrence.None, null, _now);

        result.IsSuccess.Should().BeTrue();
        var task = result.Value;
        task.Title.Should().Be("Call the lead back");
        task.Description.Should().Be("Ask about evening groups");
        task.AssigneeId.Should().Be(_managerId);
        task.DueAtUtc.Should().Be(_due);
        task.Subject.Should().Be(_lead);
        task.Recurrence.Should().Be(TaskRecurrence.None);
        task.Status.Should().Be(StaffTaskStatus.New);
        task.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new StaffTaskAssignedDomainEvent(task.Id, _managerId, null, _due, _now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyTitle_ReturnsTitleEmptyError(string? title)
    {
        var result = StaffTask.Create(title, null, _managerId, _due, null, TaskRecurrence.None, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.TitleEmpty);
    }

    [Fact]
    public void Create_EmptyAssignee_ReturnsAssigneeIdEmptyError()
    {
        var result = StaffTask.Create("Task", null, Guid.Empty, _due, null, TaskRecurrence.None, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.AssigneeIdEmpty);
    }

    [Fact]
    public void Create_DueAtCurrentTime_ReturnsDueInPastError()
    {
        var result = StaffTask.Create("Task", null, _managerId, _now, null, TaskRecurrence.None, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.DueInPast);
    }

    [Fact]
    public void Create_UndefinedRecurrence_ReturnsRecurrenceNotDefinedError()
    {
        var result = StaffTask.Create("Task", null, _managerId, _due, null, (TaskRecurrence)0, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.RecurrenceNotDefined);
    }

    [Fact]
    public void UpdateDetails_OpenTask_ChangesDetails()
    {
        var task = CreateTask();
        var newDue = _due.AddDays(1);

        var result = task.UpdateDetails("Call again", "No answer", newDue, null, _now);

        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Call again");
        task.Description.Should().Be("No answer");
        task.DueAtUtc.Should().Be(newDue);
        task.Subject.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_OverdueTaskKeepingItsDeadline_IsAllowed()
    {
        var task = CreateTask();
        var afterDue = _due.AddHours(1);

        var result = task.UpdateDetails("Call the lead back today", null, _due, _lead, afterDue);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_NewDeadlineInPast_ReturnsDueInPastError()
    {
        var task = CreateTask();

        var result = task.UpdateDetails("Call", null, _now.AddHours(-1), null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.DueInPast);
        task.DueAtUtc.Should().Be(_due);
    }

    [Fact]
    public void UpdateDetails_MovedDeadline_AllowsANewOverdueReminder()
    {
        var task = CreateTask();
        var afterDue = _due.AddHours(1);
        task.MarkOverdue(afterDue);
        task.UpdateDetails("Call", null, afterDue.AddDays(1), null, afterDue);

        var result = task.MarkOverdue(afterDue.AddDays(2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_DoneTask_ReturnsAlreadyDoneError()
    {
        var task = CreateTask();
        task.Complete(null, _now);

        var result = task.UpdateDetails("Call", null, _due, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.AlreadyDone);
    }

    [Fact]
    public void Reassign_OtherStaffMember_ChangesAssigneeAndRaisesAssignedEvent()
    {
        var task = CreateTask();
        task.ClearDomainEvents();

        var result = task.Reassign(_otherManagerId, null, _now);

        result.IsSuccess.Should().BeTrue();
        task.AssigneeId.Should().Be(_otherManagerId);
        task.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new StaffTaskAssignedDomainEvent(task.Id, _otherManagerId, null, _due, _now));
    }

    [Fact]
    public void Reassign_SameAssignee_ReturnsAlreadyAssignedError()
    {
        var result = CreateTask().Reassign(_managerId, null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.AlreadyAssigned);
    }

    [Fact]
    public void Start_NewTask_MovesToInProgress()
    {
        var task = CreateTask();

        var result = task.Start();

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.InProgress);
    }

    [Fact]
    public void Start_TaskInProgress_ReturnsNotNewError()
    {
        var task = CreateTask();
        task.Start();

        var result = task.Start();

        result.Error.Should().Be(DomainErrors.StaffTask.NotNew);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Complete_OpenTask_MarksDoneWithoutNextOccurrence(bool startFirst)
    {
        var task = CreateTask();
        if (startFirst)
            task.Start();

        var result = task.Complete(null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        task.Status.Should().Be(StaffTaskStatus.Done);
        task.CompletedAtUtc.Should().Be(_now);
    }

    [Fact]
    public void Complete_DoneTask_ReturnsAlreadyDoneError()
    {
        var task = CreateTask();
        task.Complete(null, _now);

        var result = task.Complete(null, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.AlreadyDone);
    }

    [Theory]
    [InlineData(TaskRecurrence.Daily, 2026, 4, 4)]
    [InlineData(TaskRecurrence.Weekly, 2026, 4, 10)]
    [InlineData(TaskRecurrence.Monthly, 2026, 5, 3)]
    public void Complete_RepeatingTask_ReturnsNextOccurrenceOnePeriodLater(TaskRecurrence recurrence, int year, int month, int day)
    {
        var task = CreateTask(recurrence);

        var next = task.Complete(null, _now).Value!;

        next.Id.Should().NotBe(task.Id);
        next.Title.Should().Be(task.Title);
        next.AssigneeId.Should().Be(_managerId);
        next.Subject.Should().Be(_lead);
        next.Recurrence.Should().Be(recurrence);
        next.Status.Should().Be(StaffTaskStatus.New);
        next.DueAtUtc.Should().Be(new DateTime(year, month, day, 18, 0, 0, DateTimeKind.Utc));
        next.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StaffTaskAssignedDomainEvent>();
    }

    [Fact]
    public void Complete_RepeatingTaskCompletedLate_SchedulesNextOccurrenceInTheFuture()
    {
        var task = CreateTask(TaskRecurrence.Daily);
        var completedAt = _due.AddDays(3).AddHours(1);

        var next = task.Complete(null, completedAt).Value!;

        next.DueAtUtc.Should().Be(_due.AddDays(4));
    }

    [Fact]
    public void Reopen_DoneTask_ReturnsItToWork()
    {
        var task = CreateTask();
        task.Complete(null, _now);

        var result = task.Reopen();

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.InProgress);
        task.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reopen_OpenTask_ReturnsNotDoneError()
    {
        var result = CreateTask().Reopen();

        result.Error.Should().Be(DomainErrors.StaffTask.NotDone);
    }

    [Fact]
    public void MarkOverdue_AfterDeadline_RecordsReminderAndRaisesOverdueEvent()
    {
        var task = CreateTask();
        task.ClearDomainEvents();
        var afterDue = _due.AddMinutes(1);

        var result = task.MarkOverdue(afterDue);

        result.IsSuccess.Should().BeTrue();
        task.OverdueNotifiedAtUtc.Should().Be(afterDue);
        task.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new StaffTaskOverdueDomainEvent(task.Id, _managerId, _due, afterDue));
    }

    [Fact]
    public void MarkOverdue_Twice_ReturnsOverdueAlreadyNotifiedError()
    {
        var task = CreateTask();
        task.MarkOverdue(_due.AddMinutes(1));

        var result = task.MarkOverdue(_due.AddHours(1));

        result.Error.Should().Be(DomainErrors.StaffTask.OverdueAlreadyNotified);
    }

    [Fact]
    public void MarkOverdue_BeforeDeadline_ReturnsNotOverdueError()
    {
        var result = CreateTask().MarkOverdue(_due);

        result.Error.Should().Be(DomainErrors.StaffTask.NotOverdue);
    }

    [Fact]
    public void MarkOverdue_DoneTask_ReturnsAlreadyDoneError()
    {
        var task = CreateTask();
        task.Complete(null, _now);

        var result = task.MarkOverdue(_due.AddHours(1));

        result.Error.Should().Be(DomainErrors.StaffTask.AlreadyDone);
    }

    [Fact]
    public void Create_ByAUser_RecordsTheUserInTheAssignedEvent()
    {
        var task = StaffTask.Create("Task", null, _managerId, _due, null, TaskRecurrence.None, _otherManagerId, _now).Value;

        task.DomainEvents.OfType<StaffTaskAssignedDomainEvent>().Single().AssignedByUserId.Should().Be(_otherManagerId);
    }

    [Fact]
    public void Create_EmptyCreatingUser_ReturnsUserIdEmptyError()
    {
        var result = StaffTask.Create("Task", null, _managerId, _due, null, TaskRecurrence.None, Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.UserIdEmpty);
    }

    [Fact]
    public void Reassign_EmptyUser_ReturnsUserIdEmptyErrorAndKeepsTheAssignee()
    {
        var task = CreateTask();

        var result = task.Reassign(_otherManagerId, Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.UserIdEmpty);
        task.AssigneeId.Should().Be(_managerId);
    }

    [Fact]
    public void Complete_RepeatingTask_RecordsTheCompletingUserInTheNextAssignment()
    {
        var next = CreateTask(TaskRecurrence.Weekly).Complete(_managerId, _now).Value!;

        next.DomainEvents.OfType<StaffTaskAssignedDomainEvent>().Single().AssignedByUserId.Should().Be(_managerId);
    }

    [Fact]
    public void Complete_EmptyUser_ReturnsUserIdEmptyErrorAndKeepsTheTaskOpen()
    {
        var task = CreateTask();

        var result = task.Complete(Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.StaffTask.UserIdEmpty);
        task.Status.Should().Be(StaffTaskStatus.New);
    }
}

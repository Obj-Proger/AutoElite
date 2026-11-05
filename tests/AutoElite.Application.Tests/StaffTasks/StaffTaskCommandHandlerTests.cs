using AutoElite.Application.StaffTasks;
using AutoElite.Application.StaffTasks.CompleteStaffTask;
using AutoElite.Application.StaffTasks.CreateStaffTask;
using AutoElite.Application.StaffTasks.ReassignStaffTask;
using AutoElite.Application.StaffTasks.ReopenStaffTask;
using AutoElite.Application.StaffTasks.StartStaffTask;
using AutoElite.Application.StaffTasks.UpdateStaffTask;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.StaffTasks;

public class StaffTaskCommandHandlerTests
{
    private static readonly DateTime _due = TestData.Now.AddDays(2);
    private static readonly Guid _branchId = Guid.CreateVersion7();

    private readonly InMemoryRepository<StaffTask> _tasks = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeUserRoleRepository _roles;
    private readonly FakeEntityReferences _references = new();
    private readonly User _manager;
    private readonly User _instructor;
    private readonly User _otherBranchInstructor;
    private readonly FakeCurrentUser _managerCaller;

    public StaffTaskCommandHandlerTests()
    {
        _roles = new FakeUserRoleRepository(_users);
        _manager = SeedUser("manager@example.com", _branchId, UserRole.Manager);
        _instructor = SeedUser("instructor@example.com", _branchId, UserRole.DrivingInstructor);
        _otherBranchInstructor = SeedUser("other@example.com", Guid.CreateVersion7(), UserRole.TheoryInstructor);
        _managerCaller = CallerOf(_manager, UserRole.Manager);
    }

    private User SeedUser(string email, Guid? branchId, UserRole role)
    {
        var user = TestData.NewUser(email, branchId);
        _users.Seed(user);
        _roles.Seed(user.Id, role);

        return user;
    }

    private static FakeCurrentUser CallerOf(User user, UserRole role) =>
        new() { UserId = user.Id, BranchId = user.BranchId, Roles = new HashSet<UserRole> { role } };

    private StaffTaskAccess Access(FakeCurrentUser caller) => new(_tasks, _users, caller);

    private StaffTask SeedTask(User assignee, TaskRecurrence recurrence = TaskRecurrence.None)
    {
        var task = StaffTask.Create("Call the lead back", null, assignee.Id, _due, null, recurrence, null, TestData.Now).Value;
        _tasks.Seed(task);

        return task;
    }

    private static CreateStaffTaskCommand CreateCommand(Guid assigneeId) =>
        new("Call the lead back", "Ask about evening groups", assigneeId, _due, null, null, TaskRecurrence.None);

    private Task<Result<Guid>> Create(CreateStaffTaskCommand command, FakeCurrentUser caller) =>
        new CreateStaffTaskCommandHandler(
                _tasks, new TaskAssignees(_users, _roles, caller), new TaskSubjects(_references), caller, TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);

    private Task<Result> Complete(StaffTask task, FakeCurrentUser caller) =>
        new CompleteStaffTaskCommandHandler(Access(caller), _tasks, caller, TestData.Clock())
            .Handle(new CompleteStaffTaskCommand(task.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Create_ByManagerForStaffOfTheBranch_CreatesTheTask()
    {
        var result = await Create(CreateCommand(_instructor.Id), _managerCaller);

        var task = _tasks.Items.Should().ContainSingle().Which;
        task.Id.Should().Be(result.Value);
        task.AssigneeId.Should().Be(_instructor.Id);
        task.Description.Should().Be("Ask about evening groups");
        task.DomainEvents.OfType<StaffTaskAssignedDomainEvent>().Single().AssignedByUserId.Should().Be(_manager.Id);
    }

    [Fact]
    public async Task Create_ForAStudent_ReturnsAssigneeNotStaff()
    {
        var student = SeedUser("student@example.com", _branchId, UserRole.Student);

        var result = await Create(CreateCommand(student.Id), _managerCaller);

        result.Error.Should().Be(ApplicationErrors.StaffTask.AssigneeNotStaff);
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ForADeactivatedStaffMember_ReturnsAssigneeNotStaff()
    {
        _instructor.Deactivate(TestData.Now);

        var result = await Create(CreateCommand(_instructor.Id), _managerCaller);

        result.Error.Should().Be(ApplicationErrors.StaffTask.AssigneeNotStaff);
    }

    [Fact]
    public async Task Create_ByManagerForStaffOfAnotherBranch_ReturnsAssigneeOfAnotherBranch()
    {
        var result = await Create(CreateCommand(_otherBranchInstructor.Id), _managerCaller);

        result.Error.Should().Be(ApplicationErrors.StaffTask.AssigneeOfAnotherBranch);
    }

    [Fact]
    public async Task Create_ByAdministratorForStaffOfAnyBranch_CreatesTheTask()
    {
        var result = await Create(CreateCommand(_otherBranchInstructor.Id), TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_AboutAnExistingRecord_LinksIt()
    {
        var lead = EntityReference.Create(EntityKind.Lead, Guid.CreateVersion7()).Value;
        _references.Seed(lead);

        await Create(CreateCommand(_instructor.Id) with { SubjectKind = EntityKind.Lead, SubjectId = lead.Id }, _managerCaller);

        _tasks.Items.Single().Subject.Should().Be(lead);
    }

    [Fact]
    public async Task Create_AboutAMissingRecord_ReturnsSubjectNotFound()
    {
        var command = CreateCommand(_instructor.Id) with { SubjectKind = EntityKind.Lead, SubjectId = Guid.CreateVersion7() };

        var result = await Create(command, _managerCaller);

        result.Error.Should().Be(ApplicationErrors.StaffTask.SubjectNotFound);
    }

    [Fact]
    public async Task Create_KindWithoutIdentifier_ReturnsSubjectIncomplete()
    {
        var result = await Create(CreateCommand(_instructor.Id) with { SubjectKind = EntityKind.Lead }, _managerCaller);

        result.Error.Should().Be(ApplicationErrors.StaffTask.SubjectIncomplete);
    }

    [Fact]
    public void CreateValidator_LocalDeadline_ReportsNotUtc()
    {
        var command = CreateCommand(_instructor.Id) with { DueAtUtc = DateTime.SpecifyKind(_due, DateTimeKind.Local) };

        var result = new CreateStaffTaskCommandValidator().Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(DomainErrors.TimeInterval.NotUtc.Code);
    }

    [Fact]
    public async Task Update_TaskOfStaffOutsideTheManagersBranch_ReturnsNotFound()
    {
        var task = SeedTask(_otherBranchInstructor);

        var result = await new UpdateStaffTaskCommandHandler(Access(_managerCaller), new TaskSubjects(_references), TestData.Clock())
            .Handle(new UpdateStaffTaskCommand(task.Id, "New title", null, _due, null, null), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<StaffTask>());
        task.Title.Should().Be("Call the lead back");
    }

    [Fact]
    public async Task Reassign_ToStaffOfTheBranch_RecordsWhoReassigned()
    {
        var task = SeedTask(_manager);
        task.ClearDomainEvents();

        var result = await new ReassignStaffTaskCommandHandler(
                Access(_managerCaller), new TaskAssignees(_users, _roles, _managerCaller), _managerCaller, TestData.Clock())
            .Handle(new ReassignStaffTaskCommand(task.Id, _instructor.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.AssigneeId.Should().Be(_instructor.Id);
        task.DomainEvents.OfType<StaffTaskAssignedDomainEvent>().Single().AssignedByUserId.Should().Be(_manager.Id);
    }

    [Fact]
    public async Task Start_OwnTaskOfAnInstructor_StartsIt()
    {
        var task = SeedTask(_instructor);

        var result = await new StartStaffTaskCommandHandler(Access(CallerOf(_instructor, UserRole.DrivingInstructor)))
            .Handle(new StartStaffTaskCommand(task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.InProgress);
    }

    [Fact]
    public async Task Complete_TaskOfAnotherInstructor_ReturnsNotFound()
    {
        var task = SeedTask(_otherBranchInstructor);

        var result = await Complete(task, CallerOf(_instructor, UserRole.DrivingInstructor));

        result.Error.Should().Be(ApplicationErrors.NotFound<StaffTask>());
        task.Status.Should().Be(StaffTaskStatus.New);
    }

    [Fact]
    public async Task Complete_RepeatingTask_SavesTheNextOccurrence()
    {
        var task = SeedTask(_instructor, TaskRecurrence.Weekly);

        var result = await Complete(task, CallerOf(_instructor, UserRole.DrivingInstructor));

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.Done);
        var next = _tasks.Items.Should().ContainSingle(item => item.Id != task.Id).Which;
        next.DueAtUtc.Should().Be(_due.AddDays(7));
    }

    [Fact]
    public async Task Complete_ByManagerOfTheAssigneesBranch_CompletesIt()
    {
        var task = SeedTask(_instructor);

        var result = await Complete(task, _managerCaller);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.Done);
    }

    [Fact]
    public async Task Reopen_DoneTask_ReturnsItToWork()
    {
        var task = SeedTask(_instructor);
        task.Complete(_instructor.Id, TestData.Now).IsSuccess.Should().BeTrue();

        var result = await new ReopenStaffTaskCommandHandler(Access(CallerOf(_instructor, UserRole.DrivingInstructor)))
            .Handle(new ReopenStaffTaskCommand(task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(StaffTaskStatus.InProgress);
    }
}

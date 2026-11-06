using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StaffTasks;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class StaffTaskPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(TestCurrentUser author, params object[] records)
    {
        await using var context = database.CreateContext(author, FixedClock.Default);
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static StaffTask NewTask(
        User assignee,
        DateTime dueAtUtc,
        EntityReference? subject = null,
        TaskRecurrence recurrence = TaskRecurrence.None,
        DateTime? createdAtUtc = null,
        string title = "Перезвонить клиенту") =>
        StaffTask.Create(
                title, "Уточнить удобное время занятий", assignee.Id, dueAtUtc, subject, recurrence, null,
                createdAtUtc ?? _now)
            .Value;

    [Fact]
    public async Task Save_RecurringTaskCompleted_StoresItAndTheNextOccurrence()
    {
        var assignee = TrainingRecords.Person("Орлова");
        var subject = EntityReference.Create(EntityKind.Lead, Guid.CreateVersion7()).Value;
        var task = NewTask(assignee, _now.AddDays(1), subject, TaskRecurrence.Weekly);
        await SaveAsync(TestCurrentUser.System, assignee, task);
        StaffTask? next;

        await using (var context = database.CreateContext())
        {
            var tracked = await new StaffTaskRepository(context).GetByIdAsync(task.Id, CancellationToken);
            next = tracked!.Complete(assignee.Id, _now.AddHours(2)).Value;
            new StaffTaskRepository(context).Add(next!);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var repository = new StaffTaskRepository(reader);
        var done = await repository.GetByIdAsync(task.Id, CancellationToken);
        var loadedNext = await repository.GetByIdAsync(next!.Id, CancellationToken);
        done!.Status.Should().Be(StaffTaskStatus.Done);
        done.CompletedAtUtc.Should().Be(_now.AddHours(2));
        done.Subject.Should().Be(subject);
        loadedNext!.Status.Should().Be(StaffTaskStatus.New);
        loadedNext.Subject.Should().Be(subject);
        loadedNext.Recurrence.Should().Be(TaskRecurrence.Weekly);
        loadedNext.DueAtUtc.Should().Be(next.DueAtUtc);
    }

    [Fact]
    public async Task GetOverdueToRemind_ReturnsOpenOverdueTasksNotRemindedYet_EarliestFirst()
    {
        // Years before the other tests of the class, so that their tasks are not overdue at this time.
        var createdAt = new DateTime(2020, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var checkedAt = createdAt.AddDays(10);
        var assignee = TrainingRecords.Person("Орлова");
        var earliest = NewTask(assignee, createdAt.AddDays(2), createdAtUtc: createdAt);
        var later = NewTask(assignee, createdAt.AddDays(3), createdAtUtc: createdAt);
        var reminded = NewTask(assignee, createdAt.AddDays(1), createdAtUtc: createdAt);
        reminded.MarkOverdue(createdAt.AddDays(1).AddHours(1)).IsSuccess.Should().BeTrue();
        var done = NewTask(assignee, createdAt.AddDays(1), createdAtUtc: createdAt);
        done.Complete(null, createdAt.AddHours(1)).IsSuccess.Should().BeTrue();
        var notYetDue = NewTask(assignee, checkedAt.AddDays(1), createdAtUtc: createdAt);
        await SaveAsync(TestCurrentUser.System, assignee, earliest, later, reminded, done, notYetDue);
        await using var context = database.CreateContext();
        var repository = new StaffTaskRepository(context);

        var all = await repository.GetOverdueToRemindAsync(checkedAt, 10, CancellationToken);
        var first = await repository.GetOverdueToRemindAsync(checkedAt, 1, CancellationToken);

        all.Select(task => task.Id).Should().Equal(earliest.Id, later.Id);
        first.Select(task => task.Id).Should().Equal(earliest.Id);
    }

    [Fact]
    public async Task Queries_ReadTheListAndTheCard()
    {
        var branch = TrainingRecords.NewBranch();
        var author = TrainingRecords.Person("Белова", "Анна", null);
        var assignee = User.Create(
                FullName.Create("Орлова", "Мария", null).Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value,
                null, branch.Id, _now)
            .Value;
        var subject = EntityReference.Create(EntityKind.Lead, Guid.CreateVersion7()).Value;
        var soon = NewTask(assignee, _now.AddHours(1), subject, title: "Срочно");
        var later = NewTask(assignee, _now.AddDays(2), title: "Позже");
        var started = NewTask(assignee, _now.AddDays(1), title: "В работе");
        started.Start().IsSuccess.Should().BeTrue();
        await SaveAsync(TestCurrentUser.System, branch, author, assignee);
        await SaveAsync(new TestCurrentUser { UserId = author.Id }, soon, later, started);
        await using var context = database.CreateContext();
        var queries = new StaffTaskQueries(context);
        var all = new PageRequest(1, 20);

        async Task<IEnumerable<Guid>> IdsAsync(StaffTaskListFilter filter, DataScope? scope = null, PageRequest? page = null) =>
            (await queries.GetListAsync(filter, scope ?? DataScope.SchoolWide, page ?? all, CancellationToken))
                .Items.Select(item => item.Id);

        var mine = new StaffTaskListFilter(assignee.Id, null, null, null);
        (await IdsAsync(mine)).Should().Equal(soon.Id, started.Id, later.Id);
        (await IdsAsync(mine, page: new PageRequest(2, 2))).Should().Equal(later.Id);
        (await IdsAsync(mine with { Status = StaffTaskStatus.InProgress })).Should().Equal(started.Id);
        (await IdsAsync(mine with { OverdueBeforeUtc = _now.AddHours(2) })).Should().Equal(soon.Id);
        (await IdsAsync(mine with { Subject = subject })).Should().Equal(soon.Id);
        (await IdsAsync(mine, DataScope.ForBranch(branch.Id))).Should().HaveCount(3);
        (await IdsAsync(mine, DataScope.ForBranch(Guid.CreateVersion7()))).Should().BeEmpty();
        var item = (await queries.GetListAsync(mine, DataScope.SchoolWide, all, CancellationToken)).Items[0];
        item.AssigneeName.Should().Be("Орлова Мария");
        item.Subject.Should().Be(subject);
        var card = await queries.GetDetailsAsync(later.Id, CancellationToken);
        card!.Subject.Should().BeNull();
        card.AssigneeBranchId.Should().Be(branch.Id);
        card.Description.Should().Be("Уточнить удобное время занятий");
        card.CreatedByName.Should().Be("Белова Анна");
        card.CreatedAtUtc.Should().Be(_now);
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}

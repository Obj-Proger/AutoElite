using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StudyGroups;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class StudyGroupPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly PageRequest _firstPage = new(1, PageRequest.MaxPageSize);

    private readonly string _marker = Guid.NewGuid().ToString("N")[..12];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateTime Now => FixedClock.Default.UtcNow;

    private StudyGroup NewGroup(Course course, User instructor, Guid? branchId = null, int startMonth = 2) =>
        StudyGroup.Create(
                $"Группа {_marker} {startMonth}", course, instructor.Id, branchId, new DateOnly(2026, startMonth, 1),
                new DateOnly(2026, startMonth + 3, 1), 20)
            .Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task ChangeAsync(Guid groupId, Action<StudyGroup> change)
    {
        await using var context = database.CreateContext();
        var group = await new StudyGroupRepository(context).GetByIdAsync(groupId, CancellationToken);
        change(group!);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_EnrollmentsAndWithdrawals_AreStoredWithTheGroup()
    {
        var instructor = TrainingRecords.Person("Учителев");
        var student = TrainingRecords.Person("Студентов");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var group = NewGroup(course, instructor);
        await SaveAsync(instructor, student, course, contract, group);

        await ChangeAsync(group.Id, tracked => tracked.Enroll(contract, Now).IsSuccess.Should().BeTrue());
        await using (var context = database.CreateContext())
        {
            var current = await new StudyGroupRepository(context).GetWithCurrentMemberAsync(contract.Id, CancellationToken);
            current.Should().ContainSingle().Which.CurrentMemberCount.Should().Be(1);
        }

        await ChangeAsync(group.Id, tracked => tracked.Withdraw(contract.Id, Now.AddDays(1)).IsSuccess.Should().BeTrue());

        await using var reader = database.CreateContext();
        var groups = new StudyGroupRepository(reader);
        var loaded = await groups.GetByIdAsync(group.Id, CancellationToken);
        loaded!.Enrollments.Should().ContainSingle().Which.WithdrawnAtUtc.Should().Be(Now.AddDays(1));
        loaded.CurrentMemberCount.Should().Be(0);
        (await groups.GetWithCurrentMemberAsync(contract.Id, CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task Save_SameContractEnrolledByTwoRequests_ThrowsConcurrencyConflict()
    {
        var instructor = TrainingRecords.Person("Наставников");
        var student = TrainingRecords.Person("Спешащий");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var group = NewGroup(course, instructor);
        await SaveAsync(instructor, student, course, contract, group);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        (await new StudyGroupRepository(first).GetByIdAsync(group.Id, CancellationToken))!.Enroll(contract, Now)
            .IsSuccess.Should().BeTrue();
        (await new StudyGroupRepository(second).GetByIdAsync(group.Id, CancellationToken))!.Enroll(contract, Now)
            .IsSuccess.Should().BeTrue();
        await new UnitOfWork(first).SaveChangesAsync(CancellationToken);

        var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task GetList_ShowsTheLatestStartFirstWithMemberCountsAndFilters()
    {
        var instructor = TrainingRecords.Person("Лекторов");
        var student = TrainingRecords.Person("Учащийся");
        var course = TrainingRecords.NewCourse();
        var branch = TrainingRecords.NewBranch();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var earlier = NewGroup(course, instructor, branch.Id, startMonth: 2);
        var later = NewGroup(course, instructor, startMonth: 5);
        var otherBranch = TrainingRecords.NewBranch();
        var ofOtherBranch = NewGroup(course, instructor, otherBranch.Id, startMonth: 3);
        earlier.Enroll(contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(instructor, student, course, branch, otherBranch, contract, earlier, later, ofOtherBranch);
        await using var context = database.CreateContext();
        var queries = new StudyGroupQueries(context);

        var all = await queries.GetListAsync(Filter(), DataScope.SchoolWide, _firstPage, CancellationToken);
        var scoped = await queries.GetListAsync(Filter(), DataScope.ForBranch(branch.Id), _firstPage, CancellationToken);

        all.Items.Select(item => item.Id).Should().Equal(later.Id, ofOtherBranch.Id, earlier.Id);
        all.Items.Single(item => item.Id == earlier.Id).CurrentMemberCount.Should().Be(1);
        all.Items[0].TheoryInstructorName.Should().Be("Лекторов Иван Петрович");
        all.Items[0].CourseName.Should().Be(course.Name);
        scoped.Items.Select(item => item.Id).Should().Equal(later.Id, earlier.Id);
    }

    [Fact]
    public async Task GetDetails_ReadsTheMembersWithTheirContracts()
    {
        var instructor = TrainingRecords.Person("Педагогов");
        var student = TrainingRecords.Person("Курсантов", "Илья", null);
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var group = NewGroup(course, instructor);
        group.Enroll(contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(instructor, student, course, contract, group);
        await using var context = database.CreateContext();
        var queries = new StudyGroupQueries(context);

        var details = await queries.GetDetailsAsync(group.Id, CancellationToken);

        details!.Enrollments.Should().ContainSingle().Which.Should().Match<StudyGroupMemberItem>(member =>
            member.ContractId == contract.Id && member.ContractNumber == contract.Number
            && member.StudentName == "Курсантов Илья" && member.WithdrawnAtUtc == null);
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    private StudyGroupListFilter Filter() => new(_marker, null, null, null, null);
}

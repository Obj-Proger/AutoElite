using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.TheoryLessons.GetGroupTheoryLessons;
using AutoElite.Application.TheoryLessons.GetMyTheorySchedule;
using AutoElite.Application.TheoryLessons.GetTheoryLesson;
using AutoElite.Application.TheoryLessons.GetTheorySchedule;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TheoryLessons;

public class TheoryLessonQueryHandlerTests
{
    private static readonly DateTime _from = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _to = _from.AddDays(7);

    private readonly FakeTheoryLessonQueries _queries = new();

    private static FakeCurrentUser Instructor(Guid? userId = null) =>
        new() { UserId = userId ?? Guid.CreateVersion7(), Roles = new HashSet<UserRole> { UserRole.TheoryInstructor } };

    private static TheoryLessonItem Item(Guid? branchId = null) => new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "B-2026-03", branchId, Guid.CreateVersion7(), "Road signs", Guid.CreateVersion7(),
        "Petrov Petr", _from.AddHours(10), _from.AddHours(12), TheoryLessonStatus.Scheduled);

    private static TheoryLessonDetails Details(Guid responsibleInstructorId, Guid instructorId, Guid? branchId = null) => new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "B-2026-03", branchId, responsibleInstructorId, Guid.CreateVersion7(), "Road signs",
        instructorId, "Petrov Petr", _from.AddHours(10), _from.AddHours(12), TheoryLessonStatus.Scheduled, null, []);

    private Task<Result<IReadOnlyList<TheoryLessonItem>>> GetSchedule(GetTheoryScheduleQuery query, FakeCurrentUser caller) =>
        new GetTheoryScheduleQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    private Task<Result<IReadOnlyList<TheoryLessonItem>>> GetGroupLessons(Guid groupId, FakeCurrentUser caller) =>
        new GetGroupTheoryLessonsQueryHandler(_queries, caller)
            .Handle(new GetGroupTheoryLessonsQuery(groupId), TestContext.Current.CancellationToken);

    private Task<Result<TheoryLessonDetails>> GetLesson(Guid lessonId, FakeCurrentUser caller) =>
        new GetTheoryLessonQueryHandler(_queries, caller).Handle(new GetTheoryLessonQuery(lessonId), TestContext.Current.CancellationToken);

    private Guid SeedGroup(Guid responsibleInstructorId, Guid? branchId = null)
    {
        var groupId = Guid.CreateVersion7();
        _queries.Seed(groupId, new GroupTheoryLessons(branchId, responsibleInstructorId, [Item(branchId)]));

        return groupId;
    }

    [Fact]
    public async Task GetSchedule_Manager_PassesTheFiltersWithTheScope()
    {
        var branchId = Guid.CreateVersion7();
        var instructorId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var result = await GetSchedule(
            new GetTheoryScheduleQuery(_from, _to, instructorId, groupId, branchId, TheoryLessonStatus.Completed), TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
        _queries.LastScheduleCall.Should().Be((
            new TheoryScheduleFilter(TimeInterval.Create(_from, _to).Value, instructorId, groupId, branchId, TheoryLessonStatus.Completed),
            DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task GetSchedule_Instructor_ReadsOnlyOwnLessons()
    {
        var instructor = Instructor();

        await GetSchedule(new GetTheoryScheduleQuery(_from, _to), instructor);

        _queries.LastScheduleCall!.Value.Filter.InstructorId.Should().Be(instructor.UserId!.Value);
    }

    [Fact]
    public async Task GetSchedule_InstructorAsksForAnotherInstructor_ReturnsAnEmptyScheduleWithoutReading()
    {
        var result = await GetSchedule(new GetTheoryScheduleQuery(_from, _to, InstructorId: Guid.CreateVersion7()), Instructor());

        result.Value.Should().BeEmpty();
        _queries.LastScheduleCall.Should().BeNull();
    }

    [Fact]
    public async Task GetSchedule_ManagerAsksForAnotherBranch_ReturnsAnEmptyScheduleWithoutReading()
    {
        var result = await GetSchedule(
            new GetTheoryScheduleQuery(_from, _to, BranchId: Guid.CreateVersion7()), TestData.Manager(Guid.CreateVersion7()));

        result.Value.Should().BeEmpty();
        _queries.LastScheduleCall.Should().BeNull();
    }

    [Fact]
    public async Task GetSchedule_LongestPeriod_Succeeds()
    {
        var result = await GetSchedule(new GetTheoryScheduleQuery(_from, _from.AddDays(92)), TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSchedule_PeriodLongerThanTheLimit_ReturnsPeriodTooLongWithoutReading()
    {
        var result = await GetSchedule(new GetTheoryScheduleQuery(_from, _from.AddDays(92).AddTicks(1)), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.TheoryLesson.PeriodTooLong);
        _queries.LastScheduleCall.Should().BeNull();
    }

    [Fact]
    public async Task GetSchedule_EndBeforeStart_ReturnsEndNotAfterStart()
    {
        var result = await GetSchedule(new GetTheoryScheduleQuery(_to, _from), TestData.Administrator());

        result.Error.Should().Be(DomainErrors.TimeInterval.EndNotAfterStart);
    }

    [Fact]
    public async Task GetSchedule_LocalTime_ReturnsNotUtc()
    {
        var localFrom = DateTime.SpecifyKind(_from, DateTimeKind.Local);

        var result = await GetSchedule(new GetTheoryScheduleQuery(localFrom, _to), TestData.Administrator());

        result.Error.Should().Be(DomainErrors.TimeInterval.NotUtc);
    }

    [Fact]
    public void ScheduleValidator_UndefinedStatusAndEmptyBranch_ReportsBoth()
    {
        var query = new GetTheoryScheduleQuery(_from, _to, BranchId: Guid.Empty, Status: (TheoryLessonStatus)99);

        var codes = new GetTheoryScheduleQueryValidator().Validate(query).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(ApplicationErrors.TheoryLesson.StatusNotDefined.Code, DomainErrors.StudyGroup.BranchIdEmpty.Code);
    }

    [Fact]
    public async Task GetMySchedule_Student_ReadsOwnLessonsForThePeriod()
    {
        var student = FakeCurrentUser.SignedIn(UserRole.Student);
        var lesson = new StudentTheoryLessonItem(Item(), true);
        _queries.StudentLessons.Add(lesson);

        var result = await new GetMyTheoryScheduleQueryHandler(_queries, student)
            .Handle(new GetMyTheoryScheduleQuery(_from, _to), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(lesson);
        _queries.LastStudentCall.Should().Be((student.UserId!.Value, TimeInterval.Create(_from, _to).Value));
    }

    [Fact]
    public async Task GetMySchedule_PeriodLongerThanTheLimit_ReturnsPeriodTooLong()
    {
        var result = await new GetMyTheoryScheduleQueryHandler(_queries, FakeCurrentUser.SignedIn(UserRole.Student))
            .Handle(new GetMyTheoryScheduleQuery(_from, _from.AddDays(93)), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.TheoryLesson.PeriodTooLong);
        _queries.LastStudentCall.Should().BeNull();
    }

    [Fact]
    public async Task GetGroupLessons_ResponsibleInstructor_ReadsTheLessons()
    {
        var instructor = Instructor();
        var groupId = SeedGroup(instructor.UserId!.Value);

        var result = await GetGroupLessons(groupId, instructor);

        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetGroupLessons_ManagerOfTheBranch_ReadsTheLessons()
    {
        var branchId = Guid.CreateVersion7();
        var groupId = SeedGroup(Guid.CreateVersion7(), branchId);

        var result = await GetGroupLessons(groupId, TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetGroupLessons_AnotherInstructorsGroup_ReturnsStudyGroupNotFound()
    {
        var groupId = SeedGroup(Guid.CreateVersion7());

        var result = await GetGroupLessons(groupId, Instructor());

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task GetGroupLessons_ManagerOfAnotherBranch_ReturnsStudyGroupNotFound()
    {
        var groupId = SeedGroup(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await GetGroupLessons(groupId, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task GetGroupLessons_UnknownGroup_ReturnsStudyGroupNotFound()
    {
        var result = await GetGroupLessons(Guid.CreateVersion7(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task GetLesson_SubstituteGivingTheLesson_ReadsTheCard()
    {
        var substitute = Instructor();
        var details = Details(Guid.CreateVersion7(), substitute.UserId!.Value);
        _queries.Seed(details);

        var result = await GetLesson(details.Id, substitute);

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetLesson_ResponsibleInstructorOfTheGroup_ReadsTheCard()
    {
        var responsible = Instructor();
        var details = Details(responsible.UserId!.Value, Guid.CreateVersion7());
        _queries.Seed(details);

        var result = await GetLesson(details.Id, responsible);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetLesson_ManagerOfTheBranch_ReadsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = Details(Guid.CreateVersion7(), Guid.CreateVersion7(), branchId);
        _queries.Seed(details);

        var result = await GetLesson(details.Id, TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetLesson_UninvolvedInstructor_ReturnsTheoryLessonNotFound()
    {
        var details = Details(Guid.CreateVersion7(), Guid.CreateVersion7());
        _queries.Seed(details);

        var result = await GetLesson(details.Id, Instructor());

        result.Error.Should().Be(ApplicationErrors.NotFound<TheoryLesson>());
    }

    [Fact]
    public async Task GetLesson_ManagerOfAnotherBranch_ReturnsTheoryLessonNotFound()
    {
        var details = Details(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        _queries.Seed(details);

        var result = await GetLesson(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<TheoryLesson>());
    }
}

using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Exams;
using AutoElite.Application.Exams.GetExam;
using AutoElite.Application.Exams.GetExamSchedule;
using AutoElite.Application.Exams.GetGroupExams;
using AutoElite.Application.Exams.GetMyExams;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Exams;

public class ExamQueryHandlerTests
{
    private static readonly DateTime _from = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _to = _from.AddDays(30);

    private readonly FakeExamQueries _queries = new();

    private static ExamItem Item(Guid responsibleInstructorId, Guid? branchId = null) => new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "B-2026-03", branchId, responsibleInstructorId, ExamKind.Theory,
        Guid.CreateVersion7(), "Exam B", _from.AddDays(3), ExamStatus.Scheduled, null, 0);

    private ExamDetails SeedDetails(Guid responsibleInstructorId, Guid? branchId = null)
    {
        var details = new ExamDetails(Item(responsibleInstructorId, branchId), []);
        _queries.Seed(details);

        return details;
    }

    private Task<Result<ExamDetails>> GetExam(Guid examId, FakeCurrentUser caller) =>
        new GetExamQueryHandler(_queries, caller).Handle(new GetExamQuery(examId), TestContext.Current.CancellationToken);

    private Task<Result<IReadOnlyList<ExamItem>>> GetSchedule(FakeCurrentUser caller, DateTime? to = null) =>
        new GetExamScheduleQueryHandler(_queries, caller)
            .Handle(new GetExamScheduleQuery(_from, to ?? _to, ExamKind.Practice), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetSchedule_TheoryInstructor_ReadsOnlyTheExamsOfOwnGroups()
    {
        var instructor = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);

        await GetSchedule(instructor);

        _queries.LastScheduleCall.Should().Be((
            new ExamScheduleFilter(TimeInterval.Create(_from, _to).Value, ExamKind.Practice, null, instructor.UserId),
            DataScope.SchoolWide));
    }

    [Fact]
    public async Task GetSchedule_DrivingInstructorOfABranch_ReadsTheBranchWithoutAnInstructorFilter()
    {
        var branchId = Guid.CreateVersion7();
        var instructor = new FakeCurrentUser
        {
            UserId = Guid.CreateVersion7(),
            BranchId = branchId,
            Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
        };

        await GetSchedule(instructor);

        _queries.LastScheduleCall!.Value.Filter.ResponsibleInstructorId.Should().BeNull();
        _queries.LastScheduleCall!.Value.Scope.Should().Be(DataScope.ForBranch(branchId));
    }

    [Fact]
    public async Task GetSchedule_PeriodLongerThanTheLimit_ReturnsPeriodTooLong()
    {
        var result = await GetSchedule(TestData.Administrator(), _from.AddDays(93));

        result.Error.Should().Be(ApplicationErrors.Schedule.PeriodTooLong);
        _queries.LastScheduleCall.Should().BeNull();
    }

    [Fact]
    public async Task GetGroupExams_ResponsibleInstructor_ReadsTheExams()
    {
        var instructor = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);
        var groupId = Guid.CreateVersion7();
        _queries.Seed(groupId, new GroupExams(null, instructor.UserId!.Value, [Item(instructor.UserId.Value)]));

        var result = await new GetGroupExamsQueryHandler(_queries, instructor)
            .Handle(new GetGroupExamsQuery(groupId), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetGroupExams_GroupOfAnotherInstructor_ReturnsStudyGroupNotFound()
    {
        var groupId = Guid.CreateVersion7();
        _queries.Seed(groupId, new GroupExams(null, Guid.CreateVersion7(), []));

        var result = await new GetGroupExamsQueryHandler(_queries, FakeCurrentUser.SignedIn(UserRole.TheoryInstructor))
            .Handle(new GetGroupExamsQuery(groupId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task GetExam_ManagerOfTheBranch_ReadsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = SeedDetails(Guid.CreateVersion7(), branchId);

        var result = await GetExam(details.Exam.Id, TestData.Manager(branchId));

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetExam_DrivingInstructorOfAnotherBranch_ReturnsExamNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7(), Guid.CreateVersion7());
        var instructor = new FakeCurrentUser
        {
            UserId = Guid.CreateVersion7(),
            BranchId = Guid.CreateVersion7(),
            Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
        };

        var result = await GetExam(details.Exam.Id, instructor);

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
    }

    [Fact]
    public async Task GetExam_UninvolvedTheoryInstructor_ReturnsExamNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await GetExam(details.Exam.Id, FakeCurrentUser.SignedIn(UserRole.TheoryInstructor));

        result.Error.Should().Be(ApplicationErrors.NotFound<Exam>());
    }

    [Fact]
    public async Task GetMyExams_Student_ReadsOwnExams()
    {
        var student = FakeCurrentUser.SignedIn(UserRole.Student);
        var exam = new StudentExamItem(Item(Guid.CreateVersion7()), null);
        _queries.StudentExams.Add(exam);

        var result = await new GetMyExamsQueryHandler(_queries, student)
            .Handle(new GetMyExamsQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(exam);
        _queries.LastStudentId.Should().Be(student.UserId);
    }
}

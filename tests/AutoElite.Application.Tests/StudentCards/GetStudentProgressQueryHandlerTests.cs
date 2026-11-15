using AutoElite.Application.Contracts;
using AutoElite.Application.StudentCards;
using AutoElite.Application.StudentCards.GetStudentProgress;
using AutoElite.Application.TestSessions;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StudentCards;

public class GetStudentProgressQueryHandlerTests
{
    private static readonly Guid _studentId = Guid.CreateVersion7();

    private readonly FakeStudentCardQueries _queries = new();
    private readonly FakeCurrentUser _instructor = FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

    private static StudentCardContract Contract()
    {
        var details = new ContractDetails(
            Guid.CreateVersion7(), "AE-1", _studentId, "Ivanov Ivan", Guid.CreateVersion7(), "Category B", LicenseCategory.B,
            Transmission.Manual, null, null, ContractStatus.Active, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30),
            null, null, "RUB", 45000m, 15000m, 30000m, 6m, 56, 10, 4.2m, TestData.Now);
        var attempt = new TestSessionItem(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Practice", null, LicenseCategory.B, TestSessionStatus.InProgress, null,
            TestData.Now, TestData.Now.AddMinutes(20), null, 20, 7, 1);

        return new StudentCardContract(
            details,
            new StudentCardGroup(Guid.CreateVersion7(), "B-12", 12, 10),
            new StudentCardDriving(3, 0, 2, 4.5m),
            new StudentCardTests(1, 0, 0, attempt),
            []);
    }

    private Task<Result<StudentProgress>> Handle() =>
        new GetStudentProgressQueryHandler(_queries, _instructor, TestData.Clock())
            .Handle(new GetStudentProgressQuery(_studentId), TestContext.Current.CancellationToken);

    private void SeedCard() =>
        _queries.Seed(new StudentCard(
            _studentId, "Ivanov", "Ivan", null, "ivan@example.com", "+79001234567", null, null, true, [Contract()]));

    [Fact]
    public async Task Handle_InstructorOfTheStudent_ReadsTheProgressWithTheCountsOfATestInProgressHidden()
    {
        SeedCard();
        _queries.Taught.Add((_studentId, _instructor.UserId!.Value));

        var result = await Handle();

        result.Value.Phone.Should().Be("+79001234567");
        var contract = result.Value.Contracts.Single();
        contract.CompletedPracticeHours.Should().Be(6m);
        contract.CoursePracticeHours.Should().Be(56);
        contract.Group!.LessonsAttended.Should().Be(10);
        contract.Tests.LastAttempt!.CorrectAnswerCount.Should().BeNull();
        _queries.LastUtcNow.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task Handle_InstructorWhoDoesNotTeachTheStudent_ReturnsUserNotFound()
    {
        SeedCard();

        var result = await Handle();

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
        _queries.LastUtcNow.Should().BeNull();
    }
}

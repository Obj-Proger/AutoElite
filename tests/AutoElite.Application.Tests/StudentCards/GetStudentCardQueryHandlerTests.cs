using AutoElite.Application.Contracts;
using AutoElite.Application.StudentCards;
using AutoElite.Application.StudentCards.GetStudentCard;
using AutoElite.Application.TestSessions;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StudentCards;

public class GetStudentCardQueryHandlerTests
{
    private static readonly Guid _branchId = Guid.CreateVersion7();
    private static readonly Guid _otherBranchId = Guid.CreateVersion7();
    private static readonly Guid _studentId = Guid.CreateVersion7();

    private readonly FakeStudentCardQueries _queries = new();

    private static StudentCardContract Contract(Guid? branchId, TestSessionItem? lastAttempt = null)
    {
        var details = new ContractDetails(
            Guid.CreateVersion7(), "AE-1", _studentId, "Ivanov Ivan", Guid.CreateVersion7(), "Category B", LicenseCategory.B,
            Transmission.Manual, branchId, null, ContractStatus.Active, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30),
            null, null, "RUB", 45000m, 15000m, 30000m, 6m, 56, 10, 4.2m, TestData.Now);

        return new StudentCardContract(
            details,
            new StudentCardGroup(Guid.CreateVersion7(), "B-12", 12, 10),
            new StudentCardDriving(3, 0, 2, 4.5m),
            new StudentCardTests(lastAttempt is null ? 0 : 1, 0, 0, lastAttempt),
            []);
    }

    private static TestSessionItem AttemptInProgress() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "Practice", null, LicenseCategory.B, TestSessionStatus.InProgress, null,
            TestData.Now, TestData.Now.AddMinutes(20), null, 20, 7, 1);

    private StudentCard SeedCard(Guid? studentBranchId, params StudentCardContract[] contracts)
    {
        var card = new StudentCard(
            _studentId, "Ivanov", "Ivan", null, "ivan@example.com", null, null, studentBranchId, true, contracts);
        _queries.Seed(card);

        return card;
    }

    private Task<Result<StudentCard>> Handle(FakeCurrentUser caller, Guid? studentId = null) =>
        new GetStudentCardQueryHandler(_queries, caller, TestData.Clock())
            .Handle(new GetStudentCardQuery(studentId ?? _studentId), TestContext.Current.CancellationToken);

    private static FakeCurrentUser Student(Guid userId) =>
        new() { UserId = userId, Roles = new HashSet<UserRole> { UserRole.Student } };

    [Fact]
    public async Task Handle_OwnCard_ReturnsItWithTheCountsOfATestInProgressHidden()
    {
        SeedCard(null, Contract(_branchId, AttemptInProgress()));

        var result = await Handle(Student(_studentId));

        var attempt = result.Value.Contracts.Single().Tests.LastAttempt!;
        attempt.CorrectAnswerCount.Should().BeNull();
        attempt.MistakeCount.Should().BeNull();
        _queries.LastUtcNow.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task Handle_CardOfAnotherStudent_ReturnsUserNotFound()
    {
        SeedCard(null, Contract(_branchId));

        var result = await Handle(Student(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Handle_Manager_SeesOnlyTheContractsOfTheirBranch()
    {
        var own = Contract(_branchId);
        SeedCard(null, own, Contract(_otherBranchId));

        var result = await Handle(TestData.Manager(_branchId));

        result.Value.Contracts.Should().ContainSingle().Which.Should().Be(own);
    }

    [Fact]
    public async Task Handle_ManagerOfABranchWithNoneOfTheStudentsContracts_ReturnsUserNotFound()
    {
        SeedCard(_otherBranchId, Contract(_otherBranchId));

        var result = await Handle(TestData.Manager(_branchId));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Handle_ManagerOfTheStudentsBranch_SeesTheCardEvenWithoutContracts()
    {
        SeedCard(_branchId);

        var result = await Handle(TestData.Manager(_branchId));

        result.Value.Contracts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Administrator_SeesEveryContract()
    {
        SeedCard(null, Contract(_branchId), Contract(_otherBranchId));

        var result = await Handle(TestData.Administrator());

        result.Value.Contracts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_UnknownStudent_ReturnsUserNotFound()
    {
        var result = await Handle(TestData.Administrator(), Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public void Group_LessonsMissed_IsHeldMinusAttended()
    {
        new StudentCardGroup(Guid.CreateVersion7(), "B-12", 12, 10).LessonsMissed.Should().Be(2);
    }
}

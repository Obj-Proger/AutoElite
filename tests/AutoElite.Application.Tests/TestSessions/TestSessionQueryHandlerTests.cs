using AutoElite.Application.TestSessions;
using AutoElite.Application.TestSessions.GetContractTests;
using AutoElite.Application.TestSessions.GetTestSession;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestSessions;

public class TestSessionQueryHandlerTests
{
    private readonly FakeTestSessionQueries _queries = new();
    private readonly Guid _studentId = Guid.CreateVersion7();

    private FakeCurrentUser Student() => new() { UserId = _studentId, Roles = new HashSet<UserRole> { UserRole.Student } };

    private static TestSessionItem Item(TestSessionStatus status) => new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "Exam B", null, LicenseCategory.B, status,
        status == TestSessionStatus.Failed ? TestFailureReason.TooManyMistakes : null, TestData.Now, TestData.Now.AddMinutes(20),
        status == TestSessionStatus.InProgress ? null : TestData.Now.AddMinutes(15), 20, 17, 3);

    private TestSessionDetails SeedDetails(TestSessionStatus status, Guid? branchId = null)
    {
        var correctId = Guid.CreateVersion7();
        var question = new SessionQuestionItem(
            Guid.CreateVersion7(), 1, 1, false, "What does the sign mean?", null,
            [new SessionOptionItem(correctId, "Give way"), new SessionOptionItem(Guid.CreateVersion7(), "Stop")],
            correctId, correctId, true, "Yield to the main road.");
        var details = new TestSessionDetails(Item(status), Guid.CreateVersion7(), _studentId, branchId, [question]);
        _queries.Seed(details);

        return details;
    }

    private Task<Result<TestSessionDetails>> GetSession(Guid sessionId, FakeCurrentUser caller) =>
        new GetTestSessionQueryHandler(_queries, caller).Handle(new GetTestSessionQuery(sessionId), TestContext.Current.CancellationToken);

    private Task<Result<IReadOnlyList<TestSessionItem>>> GetHistory(Guid contractId, FakeCurrentUser caller) =>
        new GetContractTestsQueryHandler(_queries, caller)
            .Handle(new GetContractTestsQuery(contractId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetSession_InProgress_HidesTheResults()
    {
        var details = SeedDetails(TestSessionStatus.InProgress);

        var result = await GetSession(details.Session.Id, Student());

        result.Value.Session.CorrectAnswerCount.Should().BeNull();
        result.Value.Session.MistakeCount.Should().BeNull();
        var question = result.Value.Questions.Should().ContainSingle().Which;
        question.Text.Should().Be("What does the sign mean?");
        question.SelectedOptionId.Should().NotBeNull();
        question.CorrectOptionId.Should().BeNull();
        question.IsCorrect.Should().BeNull();
        question.Explanation.Should().BeNull();
    }

    [Fact]
    public async Task GetSession_Finished_ShowsTheCorrectAnswersAndExplanations()
    {
        var details = SeedDetails(TestSessionStatus.Failed);

        var result = await GetSession(details.Session.Id, Student());

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetSession_ManagerOfTheContractBranch_ReadsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = SeedDetails(TestSessionStatus.Passed, branchId);

        var result = await GetSession(details.Session.Id, TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSession_AnotherStudent_ReturnsTestSessionNotFound()
    {
        var details = SeedDetails(TestSessionStatus.Passed);

        var result = await GetSession(details.Session.Id, FakeCurrentUser.SignedIn(UserRole.Student));

        result.Error.Should().Be(ApplicationErrors.NotFound<TestSession>());
    }

    [Fact]
    public async Task GetSession_ManagerOfAnotherBranch_ReturnsTestSessionNotFound()
    {
        var details = SeedDetails(TestSessionStatus.Passed, Guid.CreateVersion7());

        var result = await GetSession(details.Session.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<TestSession>());
    }

    [Fact]
    public async Task GetHistory_OwnContract_HidesOnlyTheCountsOfTheAttemptInProgress()
    {
        var contractId = Guid.CreateVersion7();
        var finished = Item(TestSessionStatus.Passed);
        var running = Item(TestSessionStatus.InProgress);
        _queries.Seed(contractId, new ContractTestSessions(_studentId, null, [running, finished]));

        var result = await GetHistory(contractId, Student());

        result.Value.Should().Equal(running with { CorrectAnswerCount = null, MistakeCount = null }, finished);
    }

    [Fact]
    public async Task GetHistory_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var contractId = Guid.CreateVersion7();
        _queries.Seed(contractId, new ContractTestSessions(_studentId, null, []));

        var result = await GetHistory(contractId, FakeCurrentUser.SignedIn(UserRole.Student));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task GetHistory_UnknownContract_ReturnsContractNotFound()
    {
        var result = await GetHistory(Guid.CreateVersion7(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }
}

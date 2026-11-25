using AutoElite.Application.Practice;
using AutoElite.Application.Practice.AnswerPracticeQuestion;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Practice;

public class AnswerPracticeQuestionCommandHandlerTests
{
    private readonly FakeContractRepository _contracts = new();
    private readonly FakeQuestionRepository _questions = new();
    private readonly FakePracticeRecordRepository _records = new();
    private readonly Contract _contract;
    private readonly FakeCurrentUser _student;
    private readonly Question _question;

    public AnswerPracticeQuestionCommandHandlerTests()
    {
        var course = TestData.NewCourse();
        var topicId = course.AddTopic("Road signs", null).Value.Id;
        _question = Question.Create(
                "Which sign?", "Because of the rule.", topicId, [LicenseCategory.B], QuestionSource.School, null,
                [new AnswerOptionDraft("Right", true), new AnswerOptionDraft("Wrong", false)])
            .Value;
        _questions.Seed(_question);
        _contract = Contract.Create(
                "AE-1", TestData.NewUser(), course, course.Price, Transmission.Manual, null, new DateOnly(2026, 2, 1),
                new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        _contracts.Seed(_contract);
        _student = new FakeCurrentUser { UserId = _contract.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };
    }

    private Guid RightOption => _question.CorrectOption.Id;

    private Guid WrongOption => _question.Options.First(option => !option.IsCorrect).Id;

    private Task<Result<PracticeAnswerResult>> Answer(Guid optionId, FakeCurrentUser? caller = null, Guid? questionId = null)
    {
        var user = caller ?? _student;

        return new AnswerPracticeQuestionCommandHandler(
                new PracticeContracts(_contracts, user), _questions, _records, user, TestData.Clock())
            .Handle(
                new AnswerPracticeQuestionCommand(_contract.Id, questionId ?? _question.Id, optionId),
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_AWrongAnswer_TellsTheCorrectOptionAndRecordsAMistake()
    {
        var result = await Answer(WrongOption);

        result.Value.Should().Be(new PracticeAnswerResult(false, RightOption, "Because of the rule."));
        var record = _records.Items.Should().ContainSingle().Subject;
        record.StudentId.Should().Be(_contract.StudentId);
        record.QuestionId.Should().Be(_question.Id);
        record.IsMistake.Should().BeTrue();
        record.LastAnsweredAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task Handle_ARightAnswerLater_UpdatesTheSameRecord()
    {
        await Answer(WrongOption);

        var result = await Answer(RightOption);

        result.Value.IsCorrect.Should().BeTrue();
        var record = _records.Items.Should().ContainSingle().Subject;
        record.AnswerCount.Should().Be(2);
        record.MistakeCount.Should().Be(1);
        record.IsMistake.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TheContractOfAnotherStudent_IsNotFound()
    {
        var stranger = new FakeCurrentUser { UserId = Guid.NewGuid(), Roles = new HashSet<UserRole> { UserRole.Student } };

        var result = await Answer(RightOption, stranger);

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
        _records.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AQuestionTakenOutOfUse_IsNotFound()
    {
        _question.Deactivate();

        var result = await Answer(RightOption);

        result.Error.Should().Be(ApplicationErrors.NotFound<Question>());
    }

    [Fact]
    public async Task Handle_AnOptionOfAnotherQuestion_IsRefused()
    {
        var result = await Answer(Guid.NewGuid());

        result.Error.Should().Be(ApplicationErrors.Practice.OptionNotInQuestion);
        _records.Items.Should().BeEmpty();
    }
}

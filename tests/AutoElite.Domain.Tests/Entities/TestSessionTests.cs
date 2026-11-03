namespace AutoElite.Domain.Tests.Entities;

public class TestSessionTests
{
    private const int BlockCount = 4;
    private const int QuestionsPerTopic = 12;

    private static readonly DateTime _now = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Currency _rub = Currency.Create("RUB").Value;

    private readonly Course _course;
    private readonly Guid[] _topicIds;
    private readonly Question[][] _bank;
    private readonly TestTemplate _examTemplate;
    private readonly Contract _contract;

    public TestSessionTests()
    {
        _course = Course.Create("Category B", LicenseCategory.B, Money.Create(45000m, _rub).Value, 134, 56, null).Value;
        _topicIds = [.. Enumerable.Range(1, BlockCount).Select(index => _course.AddTopic($"Topic {index}", null).Value.Id)];
        _bank = [.. _topicIds.Select((topicId, block) => Enumerable.Range(1, QuestionsPerTopic)
            .Select(number => CreateQuestion(topicId, $"Block {block + 1}, question {number}"))
            .ToArray())];

        TestBlock[] blocks = [.. _topicIds.Select(topicId => TestBlock.Create(5, [topicId]).Value)];
        _examTemplate = TestTemplate.Create("Exam", LicenseCategory.B, TestRules.TrafficPoliceExam, blocks, QuestionSource.Official).Value;

        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;
        _contract = Contract.Create("AE-1", student, _course, Money.Create(45000m, _rub).Value, null, _start, _end, _now).Value;
    }

    private static Question CreateQuestion(Guid topicId, string text, params LicenseCategory[] categories) =>
        Question.Create(
            text, "Because of the rules", null, topicId, categories.Length == 0 ? [LicenseCategory.B] : categories,
            QuestionSource.Official, null, [new("Right", true), new("Wrong", false), new("Also wrong", false)]).Value;

    private IReadOnlyList<IReadOnlyList<Question>> MainQuestions() =>
        [.. _bank.Select(block => (IReadOnlyList<Question>)block.Take(5).ToList())];

    private IReadOnlyList<Question> AddOnQuestions(int blockNumber, int skip = 5) =>
        [.. _bank[blockNumber - 1].Skip(skip).Take(5)];

    private TestSession StartExam() => TestSession.Start(_contract, _examTemplate, MainQuestions(), null, _now).Value;

    private static Guid Right(SessionQuestion question) => question.CorrectOptionId;

    private static Guid Wrong(SessionQuestion question) => question.Options.First(option => option.OptionId != question.CorrectOptionId).OptionId;

    private static void AnswerAll(TestSession session, Func<SessionQuestion, Guid> choose, DateTime at)
    {
        foreach (var question in session.Questions.Where(question => !question.IsAnswered).ToList())
            session.Answer(question.Id, choose(question), at);
    }

    private static SessionQuestion FirstUnanswered(TestSession session, int blockNumber) =>
        session.Questions.First(question => question.BlockNumber == blockNumber && !question.IsAnswered);

    [Fact]
    public void Start_ValidQuestions_CopiesRulesBlocksAndQuestions()
    {
        var result = TestSession.Start(_contract, _examTemplate, MainQuestions(), null, _now);

        result.IsSuccess.Should().BeTrue();
        var session = result.Value;
        session.ContractId.Should().Be(_contract.Id);
        session.StudentId.Should().Be(_contract.StudentId);
        session.TemplateId.Should().Be(_examTemplate.Id);
        session.Category.Should().Be(LicenseCategory.B);
        session.Rules.Should().Be(TestRules.TrafficPoliceExam);
        session.Blocks.Should().Equal(_examTemplate.Blocks);
        session.StartedAtUtc.Should().Be(_now);
        session.DeadlineUtc.Should().Be(_now.AddMinutes(20));
        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.IsFinished.Should().BeFalse();
        session.Questions.Should().HaveCount(20);
        session.Questions.Select(question => question.Position).Should().Equal(Enumerable.Range(1, 20));
        session.Questions.Select(question => question.BlockNumber).Should().Equal(Enumerable.Range(1, 20).Select(position => ((position - 1) / 5) + 1));
        session.Questions.Should().OnlyContain(question => !question.IsAddOn && !question.IsAnswered);
    }

    [Fact]
    public void Start_ValidQuestions_KeepsACopyThatBankEditsDoNotChange()
    {
        var session = StartExam();
        var bankQuestion = _bank[0][0];

        bankQuestion.UpdateContent("Edited text", null, null, _topicIds[0], [LicenseCategory.B], null, [new("Yes", true), new("No", false)]);

        var copy = session.Questions[0];
        copy.QuestionId.Should().Be(bankQuestion.Id);
        copy.Text.Should().Be("Block 1, question 1");
        copy.Explanation.Should().Be("Because of the rules");
        copy.Options.Select(option => option.Text).Should().Equal("Right", "Wrong", "Also wrong");
    }

    [Fact]
    public void Start_InactiveTemplate_ReturnsTemplateInactiveError()
    {
        _examTemplate.Deactivate();

        var result = TestSession.Start(_contract, _examTemplate, MainQuestions(), null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.TemplateInactive);
    }

    [Fact]
    public void Start_TerminatedContract_ReturnsContractNotActiveError()
    {
        _contract.Terminate("Moved away", _start, _now);

        var result = TestSession.Start(_contract, _examTemplate, MainQuestions(), null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.ContractNotActive);
    }

    [Fact]
    public void Start_TemplateOfAnotherCategory_ReturnsCategoryMismatchError()
    {
        TestBlock[] blocks = [.. _topicIds.Select(topicId => TestBlock.Create(5, [topicId]).Value)];
        var template = TestTemplate.Create("Exam A", LicenseCategory.A, TestRules.TrafficPoliceExam, blocks, null).Value;

        var result = TestSession.Start(_contract, template, MainQuestions(), null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.CategoryMismatch);
    }

    [Fact]
    public void Start_EmptyExamId_ReturnsExamIdEmptyError()
    {
        var result = TestSession.Start(_contract, _examTemplate, MainQuestions(), Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.TestSession.ExamIdEmpty);
    }

    [Fact]
    public void Start_QuestionsForFewerBlocks_ReturnsBlockCountMismatchError()
    {
        var result = TestSession.Start(_contract, _examTemplate, [.. MainQuestions().Take(3)], null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.BlockCountMismatch);
    }

    [Fact]
    public void Start_BlockWithTooFewQuestions_ReturnsBlockQuestionCountMismatchError()
    {
        var questions = MainQuestions().ToList();
        questions[2] = [.. _bank[2].Take(4)];

        var result = TestSession.Start(_contract, _examTemplate, questions, null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.BlockQuestionCountMismatch);
    }

    [Fact]
    public void Start_InactiveQuestion_ReturnsQuestionInactiveError()
    {
        _bank[1][0].Deactivate();

        var result = TestSession.Start(_contract, _examTemplate, MainQuestions(), null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.QuestionInactive);
    }

    [Fact]
    public void Start_QuestionOfAnotherCategory_ReturnsQuestionCategoryMismatchError()
    {
        var questions = MainQuestions().ToList();
        questions[0] = [.. _bank[0].Take(4), CreateQuestion(_topicIds[0], "Trucks only", LicenseCategory.C)];

        var result = TestSession.Start(_contract, _examTemplate, questions, null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.QuestionCategoryMismatch);
    }

    [Fact]
    public void Start_QuestionFromAnotherBlock_ReturnsQuestionOutsideBlockError()
    {
        var questions = MainQuestions().ToList();
        questions[0] = [.. _bank[0].Take(4), _bank[1][11]];

        var result = TestSession.Start(_contract, _examTemplate, questions, null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.QuestionOutsideBlock);
    }

    [Fact]
    public void Start_RepeatedQuestion_ReturnsDuplicateQuestionError()
    {
        var questions = MainQuestions().ToList();
        questions[0] = [.. _bank[0].Take(4), _bank[0][0]];

        var result = TestSession.Start(_contract, _examTemplate, questions, null, _now);

        result.Error.Should().Be(DomainErrors.TestSession.DuplicateQuestion);
    }

    [Fact]
    public void Answer_AllCorrect_PassesAndRaisesFinishedEvent()
    {
        var session = StartExam();
        var finishedAt = _now.AddMinutes(15);

        AnswerAll(session, Right, finishedAt);

        session.Status.Should().Be(TestSessionStatus.Passed);
        session.FailureReason.Should().BeNull();
        session.FinishedAtUtc.Should().Be(finishedAt);
        session.CorrectAnswerCount.Should().Be(20);
        session.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new TestSessionFinishedDomainEvent(session.Id, _contract.Id, _contract.StudentId, null, true, 20, 0, finishedAt));
    }

    [Fact]
    public void Answer_RecordsChosenOptionAndTime()
    {
        var session = StartExam();
        var question = session.Questions[0];

        var result = session.Answer(question.Id, Wrong(question), _now.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        question.SelectedOptionId.Should().Be(Wrong(question));
        question.AnsweredAtUtc.Should().Be(_now.AddMinutes(1));
        question.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void Answer_FirstMistake_AddsTimeAndWaitsForQuestionsFromTheSameBlock()
    {
        var session = StartExam();

        session.Answer(FirstUnanswered(session, 3).Id, Wrong(FirstUnanswered(session, 3)), _now.AddMinutes(1));

        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.PendingAddOnBlocks.Should().Equal(3);
        session.DeadlineUtc.Should().Be(_now.AddMinutes(25));
        session.MainMistakeCount.Should().Be(1);
    }

    [Fact]
    public void AddOnQuestions_PendingBlock_AppendsAddOnQuestionsOfThatBlock()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 3);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));

        var result = session.AddOnQuestions(3, AddOnQuestions(3));

        result.IsSuccess.Should().BeTrue();
        session.PendingAddOnBlocks.Should().BeEmpty();
        session.Questions.Should().HaveCount(25);
        session.Questions.Skip(20).Should().OnlyContain(question => question.IsAddOn && question.BlockNumber == 3);
        session.Questions.Skip(20).Select(question => question.Position).Should().Equal(21, 22, 23, 24, 25);
    }

    [Fact]
    public void Answer_OneMistakeThenEverythingElseCorrect_Passes()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 3);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));
        session.AddOnQuestions(3, AddOnQuestions(3));

        AnswerAll(session, Right, _now.AddMinutes(20));

        session.Status.Should().Be(TestSessionStatus.Passed);
        session.MainMistakeCount.Should().Be(1);
        session.CorrectAnswerCount.Should().Be(24);
    }

    [Fact]
    public void Answer_MistakesInTwoBlocks_AddsTenMinutesAndWaitsForBothBlocks()
    {
        var session = StartExam();
        var first = FirstUnanswered(session, 1);
        session.Answer(first.Id, Wrong(first), _now.AddMinutes(1));
        var second = FirstUnanswered(session, 4);

        session.Answer(second.Id, Wrong(second), _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.PendingAddOnBlocks.Should().Equal(1, 4);
        session.DeadlineUtc.Should().Be(_now.AddMinutes(30));
    }

    [Fact]
    public void Answer_TwoMistakesInOneBlock_FailsWithTooManyMistakesInBlock()
    {
        var session = StartExam();
        var first = FirstUnanswered(session, 2);
        session.Answer(first.Id, Wrong(first), _now.AddMinutes(1));
        var second = FirstUnanswered(session, 2);

        session.Answer(second.Id, Wrong(second), _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.TooManyMistakesInBlock);
        session.PendingAddOnBlocks.Should().BeEmpty();
        session.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void Answer_ThirdMistake_FailsWithTooManyMistakes()
    {
        var session = StartExam();
        foreach (var block in new[] { 1, 2 })
        {
            var question = FirstUnanswered(session, block);
            session.Answer(question.Id, Wrong(question), _now.AddMinutes(1));
        }

        var third = FirstUnanswered(session, 3);
        session.Answer(third.Id, Wrong(third), _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.TooManyMistakes);
    }

    [Fact]
    public void Answer_MistakeInAddOnQuestion_FailsWithMistakeInAddOnQuestion()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 1);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));
        session.AddOnQuestions(1, AddOnQuestions(1));
        var addOn = session.Questions[20];

        session.Answer(addOn.Id, Wrong(addOn), _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.MistakeInAddOnQuestion);
        session.MainMistakeCount.Should().Be(1);
    }

    [Fact]
    public void Answer_FinishedWithFailure_RaisesFinishedEventWithMistakes()
    {
        var session = StartExam();
        var first = FirstUnanswered(session, 2);
        session.Answer(first.Id, Wrong(first), _now.AddMinutes(1));
        var second = FirstUnanswered(session, 2);

        session.Answer(second.Id, Wrong(second), _now.AddMinutes(2));

        session.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new TestSessionFinishedDomainEvent(session.Id, _contract.Id, _contract.StudentId, null, false, 0, 2, _now.AddMinutes(2)));
    }

    [Fact]
    public void Answer_LastMainQuestionWrong_DoesNotPassWhileAddOnsArePending()
    {
        var session = StartExam();
        var last = session.Questions[^1];
        foreach (var question in session.Questions.Where(question => question.Id != last.Id).ToList())
            session.Answer(question.Id, Right(question), _now.AddMinutes(1));

        session.Answer(last.Id, Wrong(last), _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.PendingAddOnBlocks.Should().Equal(4);
    }

    [Fact]
    public void Answer_RulesWithoutAddOns_AllowsMistakesWithinTheLimitAndPasses()
    {
        var rules = TestRules.Create(5, TimeSpan.FromMinutes(5), 1, null, 0, TimeSpan.Zero).Value;
        var template = TestTemplate.Create("Topic test", LicenseCategory.B, rules, [TestBlock.Create(5, [_topicIds[0]]).Value], null).Value;
        var session = TestSession.Start(_contract, template, [_bank[0].Take(5).ToList()], null, _now).Value;
        var wrong = session.Questions[0];
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));

        AnswerAll(session, Right, _now.AddMinutes(2));

        session.Status.Should().Be(TestSessionStatus.Passed);
        session.PendingAddOnBlocks.Should().BeEmpty();
        session.DeadlineUtc.Should().Be(_now.AddMinutes(5));
    }

    [Fact]
    public void Answer_AtDeadline_ReturnsTimeExpiredErrorWithoutRecording()
    {
        var session = StartExam();
        var question = session.Questions[0];

        var result = session.Answer(question.Id, Right(question), session.DeadlineUtc);

        result.Error.Should().Be(DomainErrors.TestSession.TimeExpired);
        question.IsAnswered.Should().BeFalse();
    }

    [Fact]
    public void Answer_AnsweredQuestion_ReturnsAlreadyAnsweredError()
    {
        var session = StartExam();
        var question = session.Questions[0];
        session.Answer(question.Id, Right(question), _now.AddMinutes(1));

        var result = session.Answer(question.Id, Wrong(question), _now.AddMinutes(2));

        result.Error.Should().Be(DomainErrors.TestSession.AlreadyAnswered);
        question.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void Answer_UnknownQuestion_ReturnsQuestionNotInSessionError()
    {
        var session = StartExam();

        var result = session.Answer(Guid.CreateVersion7(), Guid.CreateVersion7(), _now.AddMinutes(1));

        result.Error.Should().Be(DomainErrors.TestSession.QuestionNotInSession);
    }

    [Fact]
    public void Answer_OptionOfAnotherQuestion_ReturnsOptionNotInQuestionError()
    {
        var session = StartExam();

        var result = session.Answer(session.Questions[0].Id, Right(session.Questions[1]), _now.AddMinutes(1));

        result.Error.Should().Be(DomainErrors.TestSession.OptionNotInQuestion);
    }

    [Fact]
    public void Answer_FinishedSession_ReturnsNotInProgressError()
    {
        var session = StartExam();
        AnswerAll(session, Right, _now.AddMinutes(1));

        var result = session.Answer(session.Questions[0].Id, Right(session.Questions[0]), _now.AddMinutes(2));

        result.Error.Should().Be(DomainErrors.TestSession.NotInProgress);
    }

    [Fact]
    public void AddOnQuestions_NotPending_ReturnsAddOnNotPendingError()
    {
        var session = StartExam();

        var result = session.AddOnQuestions(1, AddOnQuestions(1));

        result.Error.Should().Be(DomainErrors.TestSession.AddOnNotPending);
    }

    [Fact]
    public void AddOnQuestions_WrongCount_ReturnsAddOnCountMismatchError()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 1);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));

        var result = session.AddOnQuestions(1, [.. AddOnQuestions(1).Take(4)]);

        result.Error.Should().Be(DomainErrors.TestSession.AddOnCountMismatch);
        session.PendingAddOnBlocks.Should().Equal(1);
    }

    [Fact]
    public void AddOnQuestions_QuestionAlreadyShown_ReturnsDuplicateQuestionError()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 1);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));

        var result = session.AddOnQuestions(1, AddOnQuestions(1, skip: 4));

        result.Error.Should().Be(DomainErrors.TestSession.DuplicateQuestion);
        session.Questions.Should().HaveCount(20);
    }

    [Fact]
    public void AddOnQuestions_QuestionsOfAnotherBlock_ReturnsQuestionOutsideBlockError()
    {
        var session = StartExam();
        var wrong = FirstUnanswered(session, 1);
        session.Answer(wrong.Id, Wrong(wrong), _now.AddMinutes(1));

        var result = session.AddOnQuestions(1, AddOnQuestions(2));

        result.Error.Should().Be(DomainErrors.TestSession.QuestionOutsideBlock);
    }

    [Fact]
    public void Expire_AfterDeadline_FailsWithTimeExpired()
    {
        var session = StartExam();
        session.Answer(session.Questions[0].Id, Right(session.Questions[0]), _now.AddMinutes(1));

        var result = session.Expire(session.DeadlineUtc);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(TestSessionStatus.Failed);
        session.FailureReason.Should().Be(TestFailureReason.TimeExpired);
        session.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TestSessionFinishedDomainEvent>();
    }

    [Fact]
    public void Expire_BeforeDeadline_ReturnsTimeNotExpiredError()
    {
        var session = StartExam();

        var result = session.Expire(session.DeadlineUtc.AddTicks(-1));

        result.Error.Should().Be(DomainErrors.TestSession.TimeNotExpired);
        session.Status.Should().Be(TestSessionStatus.InProgress);
    }

    [Fact]
    public void Expire_FinishedSession_ReturnsNotInProgressError()
    {
        var session = StartExam();
        AnswerAll(session, Right, _now.AddMinutes(1));

        var result = session.Expire(_now.AddHours(1));

        result.Error.Should().Be(DomainErrors.TestSession.NotInProgress);
    }
}

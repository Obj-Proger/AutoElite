using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// One attempt of a student at a test (FR-7.5..FR-7.8): a copy of the rules, the blocks and every question shown, the
/// answers, and the outcome.
/// </summary>
/// <remarks>
/// <para>
/// The application layer draws the questions at random and passes them in; the session checks that they fit the
/// template and never repeat within the attempt. After a mistake in a main question that the rules allow, the session
/// waits for <see cref="TestRules.ExtraQuestionsPerMistake"/> more questions from the same block
/// (<see cref="PendingAddOnBlocks"/>), and the deadline moves by <see cref="TestRules.ExtraTimePerMistake"/>.
/// </para>
/// <para>
/// The attempt fails as soon as a rule is broken, and passes when every question has been answered and no added
/// questions are pending. When the deadline passes, <see cref="Expire"/> fails it. Correct answers and explanations
/// are part of the copies, but the application layer shows them only once the attempt is finished (FR-7.7).
/// </para>
/// </remarks>
public sealed class TestSession : AuditableEntity
{
    // Not read-only, so that the persistence layer can load the stored blocks into it.
    private List<TestBlock> _blocks = [];
    private readonly List<SessionQuestion> _questions = [];
    // Not read-only, so that the persistence layer can load the stored list into it.
    private List<int> _pendingAddOnBlocks = [];

    /// <summary>Gets the identifier of the student's contract.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student, copied from the contract.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the identifier of the template the attempt was started from.</summary>
    public Guid TemplateId { get; private set; }

    /// <summary>Gets the identifier of the exam the attempt belongs to, or <see langword="null"/> for practice.</summary>
    public Guid? ExamId { get; private set; }

    /// <summary>Gets the licence category of the questions, copied from the template.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the rules, copied from the template when the attempt started.</summary>
    public TestRules Rules { get; private set; } = null!;

    /// <summary>Gets the blocks, copied from the template when the attempt started.</summary>
    public IReadOnlyList<TestBlock> Blocks => _blocks.AsReadOnly();

    /// <summary>Gets the questions in the order they were shown, main and added ones.</summary>
    public IReadOnlyList<SessionQuestion> Questions => _questions.AsReadOnly();

    /// <summary>Gets the numbers of the blocks still waiting for added questions, one entry per mistake.</summary>
    public IReadOnlyList<int> PendingAddOnBlocks => _pendingAddOnBlocks.AsReadOnly();

    /// <summary>Gets the UTC time the attempt started.</summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>Gets the UTC time by which every question must be answered, including the time added for mistakes.</summary>
    public DateTime DeadlineUtc { get; private set; }

    /// <summary>Gets the status of the attempt.</summary>
    public TestSessionStatus Status { get; private set; }

    /// <summary>Gets why the attempt failed, or <see langword="null"/> if it did not.</summary>
    public TestFailureReason? FailureReason { get; private set; }

    /// <summary>Gets the UTC time the attempt finished, if it has.</summary>
    public DateTime? FinishedAtUtc { get; private set; }

    /// <summary>Gets the number of wrong answers in main questions.</summary>
    public int MainMistakeCount => _questions.Count(question => !question.IsAddOn && question.IsCorrect == false);

    /// <summary>Gets the number of correct answers.</summary>
    public int CorrectAnswerCount => _questions.Count(question => question.IsCorrect == true);

    /// <summary>Gets a value indicating whether the attempt is finished, so that answers and explanations may be shown.</summary>
    public bool IsFinished => Status != TestSessionStatus.InProgress;

    private TestSession(Guid id, Contract contract, TestTemplate template, Guid? examId, DateTime utcNow) : base(id)
    {
        ContractId = contract.Id;
        StudentId = contract.StudentId;
        TemplateId = template.Id;
        ExamId = examId;
        Category = template.Category;
        Rules = template.Rules;
        _blocks.AddRange(template.Blocks);
        StartedAtUtc = utcNow;
        DeadlineUtc = utcNow + template.Rules.TimeLimit;
        Status = TestSessionStatus.InProgress;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private TestSession()
    {
    }

    /// <summary>Starts an attempt with the questions drawn for each block of the template.</summary>
    /// <param name="contract">The student's contract; it must be active and of the template's licence category.</param>
    /// <param name="template">The template; it must be active.</param>
    /// <param name="questionsByBlock">For each block, in order, exactly its number of questions, in the order to show them.</param>
    /// <param name="examId">The exam the attempt belongs to, or <see langword="null"/> for practice.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The attempt, or the first validation failure encountered.</returns>
    public static Result<TestSession> Start(
        Contract contract,
        TestTemplate template,
        IReadOnlyList<IReadOnlyList<Question>> questionsByBlock,
        Guid? examId,
        DateTime utcNow)
    {
        if (!template.IsActive)
            return Result.Failure<TestSession>(DomainErrors.TestSession.TemplateInactive);

        if (contract.Status != ContractStatus.Active)
            return Result.Failure<TestSession>(DomainErrors.TestSession.ContractNotActive);

        if (contract.Category != template.Category)
            return Result.Failure<TestSession>(DomainErrors.TestSession.CategoryMismatch);

        if (examId is { } exam && exam == Guid.Empty)
            return Result.Failure<TestSession>(DomainErrors.TestSession.ExamIdEmpty);

        if (questionsByBlock.Count != template.Blocks.Count)
            return Result.Failure<TestSession>(DomainErrors.TestSession.BlockCountMismatch);

        var session = new TestSession(Guid.CreateVersion7(), contract, template, examId, utcNow);

        for (var index = 0; index < questionsByBlock.Count; index++)
        {
            var blockResult = session.AddQuestions(index + 1, questionsByBlock[index], template.Blocks[index].QuestionCount, isAddOn: false);
            if (blockResult.IsFailure)
                return Result.Failure<TestSession>(blockResult.Error);
        }

        return session;
    }

    /// <summary>Records the answer to one question and applies the rules.</summary>
    /// <param name="sessionQuestionId">The question of this attempt.</param>
    /// <param name="optionId">The chosen option of that question.</param>
    /// <param name="utcNow">The current UTC time; it must be before the deadline.</param>
    /// <returns>A failure if the attempt is finished or out of time, or the question or option is not valid.</returns>
    public Result Answer(Guid sessionQuestionId, Guid optionId, DateTime utcNow)
    {
        if (Status != TestSessionStatus.InProgress)
            return Result.Failure(DomainErrors.TestSession.NotInProgress);

        if (utcNow >= DeadlineUtc)
            return Result.Failure(DomainErrors.TestSession.TimeExpired);

        var question = _questions.Find(candidate => candidate.Id == sessionQuestionId);
        if (question is null)
            return Result.Failure(DomainErrors.TestSession.QuestionNotInSession);

        if (question.IsAnswered)
            return Result.Failure(DomainErrors.TestSession.AlreadyAnswered);

        if (!question.HasOption(optionId))
            return Result.Failure(DomainErrors.TestSession.OptionNotInQuestion);

        question.Answer(optionId, utcNow);

        if (question.IsCorrect == false)
            ApplyMistake(question, utcNow);

        if (Status == TestSessionStatus.InProgress && _pendingAddOnBlocks.Count == 0 && _questions.TrueForAll(candidate => candidate.IsAnswered))
            Finish(TestSessionStatus.Passed, null, utcNow);

        return Result.Success();
    }

    /// <summary>Adds the questions required after a mistake in the given block.</summary>
    /// <param name="blockNumber">A block listed in <see cref="PendingAddOnBlocks"/>.</param>
    /// <param name="questions">Exactly <see cref="TestRules.ExtraQuestionsPerMistake"/> questions of that block not shown in this attempt yet.</param>
    /// <returns>A failure if the attempt is finished, no questions are pending for the block, or the questions do not fit.</returns>
    public Result AddOnQuestions(int blockNumber, IReadOnlyList<Question> questions)
    {
        if (Status != TestSessionStatus.InProgress)
            return Result.Failure(DomainErrors.TestSession.NotInProgress);

        if (!_pendingAddOnBlocks.Contains(blockNumber))
            return Result.Failure(DomainErrors.TestSession.AddOnNotPending);

        var result = AddQuestions(blockNumber, questions, Rules.ExtraQuestionsPerMistake, isAddOn: true);
        if (result.IsFailure)
            return result;

        _pendingAddOnBlocks.Remove(blockNumber);

        return Result.Success();
    }

    /// <summary>Fails an attempt whose deadline has passed.</summary>
    /// <param name="utcNow">The current UTC time; it must be at or after the deadline.</param>
    /// <returns>A failure if the attempt is finished or still has time.</returns>
    public Result Expire(DateTime utcNow)
    {
        if (Status != TestSessionStatus.InProgress)
            return Result.Failure(DomainErrors.TestSession.NotInProgress);

        if (utcNow < DeadlineUtc)
            return Result.Failure(DomainErrors.TestSession.TimeNotExpired);

        Finish(TestSessionStatus.Failed, TestFailureReason.TimeExpired, utcNow);

        return Result.Success();
    }

    private void ApplyMistake(SessionQuestion question, DateTime utcNow)
    {
        if (question.IsAddOn)
        {
            Finish(TestSessionStatus.Failed, TestFailureReason.MistakeInAddOnQuestion, utcNow);
            return;
        }

        if (MainMistakeCount > Rules.MaxMistakes)
        {
            Finish(TestSessionStatus.Failed, TestFailureReason.TooManyMistakes, utcNow);
            return;
        }

        var blockMistakes = _questions.Count(candidate =>
            !candidate.IsAddOn && candidate.BlockNumber == question.BlockNumber && candidate.IsCorrect == false);

        if (Rules.MaxMistakesPerBlock is { } perBlock && blockMistakes > perBlock)
        {
            Finish(TestSessionStatus.Failed, TestFailureReason.TooManyMistakesInBlock, utcNow);
            return;
        }

        if (Rules.HasAddOnQuestions)
        {
            _pendingAddOnBlocks.Add(question.BlockNumber);
            DeadlineUtc += Rules.ExtraTimePerMistake;
        }
    }

    private Result AddQuestions(int blockNumber, IReadOnlyList<Question> questions, int expectedCount, bool isAddOn)
    {
        if (questions.Count != expectedCount)
            return Result.Failure(isAddOn ? DomainErrors.TestSession.AddOnCountMismatch : DomainErrors.TestSession.BlockQuestionCountMismatch);

        var block = _blocks[blockNumber - 1];

        foreach (var question in questions)
        {
            if (!question.IsActive)
                return Result.Failure(DomainErrors.TestSession.QuestionInactive);

            if (!question.Categories.Contains(Category))
                return Result.Failure(DomainErrors.TestSession.QuestionCategoryMismatch);

            if (!block.CoversAllTopics && !block.TopicIds.Contains(question.TopicId))
                return Result.Failure(DomainErrors.TestSession.QuestionOutsideBlock);
        }

        if (questions.Select(question => question.Id).Distinct().Count() != questions.Count
            || questions.Any(question => _questions.Exists(shown => shown.QuestionId == question.Id)))
            return Result.Failure(DomainErrors.TestSession.DuplicateQuestion);

        foreach (var question in questions)
            _questions.Add(SessionQuestion.CopyOf(question, _questions.Count + 1, blockNumber, isAddOn));

        return Result.Success();
    }

    private void Finish(TestSessionStatus status, TestFailureReason? reason, DateTime utcNow)
    {
        Status = status;
        FailureReason = reason;
        FinishedAtUtc = utcNow;
        _pendingAddOnBlocks.Clear();
        RaiseDomainEvent(new TestSessionFinishedDomainEvent(
            Id, ContractId, StudentId, ExamId, status == TestSessionStatus.Passed, CorrectAnswerCount, MainMistakeCount, utcNow));
    }
}

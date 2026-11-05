namespace AutoElite.Application.TestSessions;

/// <summary>An attempt in the test history of a contract.</summary>
/// <param name="Id">The identifier of the attempt.</param>
/// <param name="TemplateId">The template.</param>
/// <param name="TemplateName">The name of the template.</param>
/// <param name="ExamId">The exam the attempt belongs to, or <see langword="null"/> for a practice test.</param>
/// <param name="Category">The licence category.</param>
/// <param name="Status">The status of the attempt.</param>
/// <param name="FailureReason">Why the attempt failed, if it did.</param>
/// <param name="StartedAtUtc">When the attempt started.</param>
/// <param name="DeadlineUtc">When the time of the attempt runs out, extra time included.</param>
/// <param name="FinishedAtUtc">When the attempt finished, if it did.</param>
/// <param name="QuestionCount">The number of questions shown, added ones included.</param>
/// <param name="CorrectAnswerCount">The correct answers; hidden while the attempt is in progress.</param>
/// <param name="MistakeCount">The mistakes in the main questions; hidden while the attempt is in progress.</param>
public sealed record TestSessionItem(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    Guid? ExamId,
    LicenseCategory Category,
    TestSessionStatus Status,
    TestFailureReason? FailureReason,
    DateTime StartedAtUtc,
    DateTime DeadlineUtc,
    DateTime? FinishedAtUtc,
    int QuestionCount,
    int? CorrectAnswerCount,
    int? MistakeCount);

/// <summary>The attempts of a contract with what is needed to decide who may read them.</summary>
/// <param name="StudentId">The student of the contract.</param>
/// <param name="BranchId">The branch of the contract, if any.</param>
/// <param name="Sessions">All attempts of the contract, the latest first.</param>
public sealed record ContractTestSessions(Guid StudentId, Guid? BranchId, IReadOnlyList<TestSessionItem> Sessions);

/// <summary>An answer option of a question of an attempt.</summary>
/// <param name="OptionId">The identifier of the option.</param>
/// <param name="Text">The text, as it was when the question was drawn.</param>
public sealed record SessionOptionItem(Guid OptionId, string Text);

/// <summary>A question of an attempt.</summary>
/// <param name="Id">The identifier of the question within the attempt, used to answer it.</param>
/// <param name="Position">The position in the attempt, from 1.</param>
/// <param name="BlockNumber">The block, from 1.</param>
/// <param name="IsAddOn">Whether the question was added for a mistake.</param>
/// <param name="Text">The text, as it was when the question was drawn.</param>
/// <param name="ImageDocumentId">The document with the image, if any; anyone signed in can download it.</param>
/// <param name="Options">The answer options in order.</param>
/// <param name="SelectedOptionId">The chosen option, if answered.</param>
/// <param name="CorrectOptionId">The correct option; hidden while the attempt is in progress.</param>
/// <param name="IsCorrect">Whether the answer is correct; hidden while the attempt is in progress.</param>
/// <param name="Explanation">The explanation; hidden while the attempt is in progress.</param>
public sealed record SessionQuestionItem(
    Guid Id,
    int Position,
    int BlockNumber,
    bool IsAddOn,
    string Text,
    Guid? ImageDocumentId,
    IReadOnlyList<SessionOptionItem> Options,
    Guid? SelectedOptionId,
    Guid? CorrectOptionId,
    bool? IsCorrect,
    string? Explanation);

/// <summary>The card of an attempt with its questions.</summary>
/// <param name="Session">The attempt.</param>
/// <param name="ContractId">The contract.</param>
/// <param name="StudentId">The student.</param>
/// <param name="BranchId">The branch of the contract, if any.</param>
/// <param name="Questions">The questions in the order shown.</param>
public sealed record TestSessionDetails(
    TestSessionItem Session,
    Guid ContractId,
    Guid StudentId,
    Guid? BranchId,
    IReadOnlyList<SessionQuestionItem> Questions);

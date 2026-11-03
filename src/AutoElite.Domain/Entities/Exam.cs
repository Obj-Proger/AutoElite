using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Domain.Entities;

/// <summary>
/// An internal school exam of a study group (FR-8.1..FR-8.6): a theory exam taken as a test from a template, or a
/// practical driving exam graded by an instructor, with one result per student.
/// </summary>
/// <remarks>
/// Results are recorded once the exam has started and only for current members of the group: a theory result is taken
/// from the student's finished test attempt for this exam, a practical result is entered by the instructor. Any result
/// can be corrected later. An exam with results can no longer be moved or cancelled.
/// </remarks>
public sealed class Exam : AuditableEntity
{
    /// <summary>The highest score that can be given.</summary>
    public const int MaxScore = 1000;

    private const int MaxCommentLength = 2000;
    private const int MaxCancellationReasonLength = 1000;

    private readonly List<ExamResult> _results = [];

    /// <summary>Gets the identifier of the study group.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the kind of the exam.</summary>
    public ExamKind Kind { get; private set; }

    /// <summary>Gets the identifier of the test template of a theory exam, or <see langword="null"/> for a practical exam.</summary>
    public Guid? TestTemplateId { get; private set; }

    /// <summary>Gets the UTC time the exam starts.</summary>
    public DateTime ScheduledAtUtc { get; private set; }

    /// <summary>Gets the exam status.</summary>
    public ExamStatus Status { get; private set; }

    /// <summary>Gets the reason the exam was cancelled, if it was.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>Gets the results in the order they were recorded.</summary>
    public IReadOnlyList<ExamResult> Results => _results.AsReadOnly();

    private Exam(Guid id, StudyGroup group, ExamKind kind, Guid? testTemplateId, DateTime scheduledAtUtc) : base(id)
    {
        GroupId = group.Id;
        Kind = kind;
        TestTemplateId = testTemplateId;
        ScheduledAtUtc = scheduledAtUtc;
        Status = ExamStatus.Scheduled;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Exam()
    {
    }

    /// <summary>Schedules an exam for a group and raises <see cref="ExamScheduledDomainEvent"/> (FR-8.1, FR-8.6).</summary>
    /// <param name="group">The study group; it must be active.</param>
    /// <param name="kind">Theory or practice.</param>
    /// <param name="testTemplate">For a theory exam, an active template of the group's category; for a practical exam, <see langword="null"/>.</param>
    /// <param name="scheduledAtUtc">When the exam starts; it must be in the future.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The exam, or the first validation failure encountered.</returns>
    public static Result<Exam> Schedule(
        StudyGroup group, ExamKind kind, TestTemplate? testTemplate, DateTime scheduledAtUtc, DateTime utcNow)
    {
        if (!group.IsActive)
            return Result.Failure<Exam>(DomainErrors.Exam.GroupInactive);

        var kindResult = Guard.DefinedEnum(kind, DomainErrors.Exam.KindNotDefined);
        if (kindResult.IsFailure)
            return Result.Failure<Exam>(kindResult.Error);

        var templateResult = ValidateTemplate(group, kind, testTemplate);
        if (templateResult.IsFailure)
            return Result.Failure<Exam>(templateResult.Error);

        if (scheduledAtUtc <= utcNow)
            return Result.Failure<Exam>(DomainErrors.Exam.ScheduledInPast);

        var exam = new Exam(Guid.CreateVersion7(), group, kind, testTemplate?.Id, scheduledAtUtc);
        exam.RaiseDomainEvent(new ExamScheduledDomainEvent(exam.Id, group.Id, kind, scheduledAtUtc, utcNow));

        return exam;
    }

    /// <summary>Moves an exam without results and raises <see cref="ExamRescheduledDomainEvent"/> (FR-8.2).</summary>
    /// <param name="scheduledAtUtc">The new start; it must be in the future.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the exam is cancelled, has results, or the time is in the past.</returns>
    public Result Reschedule(DateTime scheduledAtUtc, DateTime utcNow)
    {
        var changeResult = EnsureCanChangeSchedule();
        if (changeResult.IsFailure)
            return changeResult;

        if (scheduledAtUtc <= utcNow)
            return Result.Failure(DomainErrors.Exam.ScheduledInPast);

        ScheduledAtUtc = scheduledAtUtc;
        RaiseDomainEvent(new ExamRescheduledDomainEvent(Id, GroupId, scheduledAtUtc, utcNow));

        return Result.Success();
    }

    /// <summary>Cancels an exam without results and raises <see cref="ExamCancelledDomainEvent"/>.</summary>
    /// <param name="reason">Why the exam is cancelled. Required.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the exam is cancelled, has results, or the reason is invalid.</returns>
    public Result Cancel(string? reason, DateTime utcNow)
    {
        var changeResult = EnsureCanChangeSchedule();
        if (changeResult.IsFailure)
            return changeResult;

        var reasonResult = Guard.RequiredText(
            reason, MaxCancellationReasonLength, DomainErrors.Exam.CancellationReasonEmpty, DomainErrors.Exam.CancellationReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        Status = ExamStatus.Cancelled;
        CancellationReason = reasonResult.Value;
        RaiseDomainEvent(new ExamCancelledDomainEvent(Id, GroupId, reasonResult.Value, utcNow));

        return Result.Success();
    }

    /// <summary>Records the theory result of a student from their finished test attempt for this exam (FR-8.3).</summary>
    /// <param name="group">The exam's group, used to check membership.</param>
    /// <param name="session">The student's finished attempt started for this exam.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The result, or a failure if the exam is not a theory exam, the attempt does not fit, or the student already has a result.</returns>
    public Result<ExamResult> RecordTheoryResult(StudyGroup group, TestSession session, DateTime utcNow)
    {
        if (Kind != ExamKind.Theory)
            return Result.Failure<ExamResult>(DomainErrors.Exam.WrongKind);

        if (session.ExamId != Id)
            return Result.Failure<ExamResult>(DomainErrors.Exam.SessionNotForExam);

        if (!session.IsFinished)
            return Result.Failure<ExamResult>(DomainErrors.Exam.SessionNotFinished);

        var recordResult = EnsureCanRecord(group, session.ContractId, utcNow);
        if (recordResult.IsFailure)
            return Result.Failure<ExamResult>(recordResult.Error);

        var result = ExamResult.Record(
            session.ContractId,
            session.StudentId,
            session.Status == TestSessionStatus.Passed,
            session.CorrectAnswerCount,
            comment: null,
            session.Id,
            recordedByUserId: null,
            utcNow);

        return AddResult(result, utcNow);
    }

    /// <summary>Records the practical result of a student, entered by the instructor (FR-8.4).</summary>
    /// <param name="group">The exam's group, used to check membership.</param>
    /// <param name="contract">The student's contract.</param>
    /// <param name="passed">Whether the student passed.</param>
    /// <param name="score">The points, from 0 to <see cref="MaxScore"/>, if given.</param>
    /// <param name="comment">An optional comment.</param>
    /// <param name="recordedByUserId">The instructor entering the result.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The result, or a failure if the exam is not a practical exam, an argument is invalid, or the student already has a result.</returns>
    public Result<ExamResult> RecordPracticeResult(
        StudyGroup group, Contract contract, bool passed, int? score, string? comment, Guid recordedByUserId, DateTime utcNow)
    {
        if (Kind != ExamKind.Practice)
            return Result.Failure<ExamResult>(DomainErrors.Exam.WrongKind);

        var recordResult = EnsureCanRecord(group, contract.Id, utcNow);
        if (recordResult.IsFailure)
            return Result.Failure<ExamResult>(recordResult.Error);

        var valuesResult = ValidateResultValues(score, comment, recordedByUserId);
        if (valuesResult.IsFailure)
            return Result.Failure<ExamResult>(valuesResult.Error);

        var result = ExamResult.Record(contract.Id, contract.StudentId, passed, score, valuesResult.Value, null, recordedByUserId, utcNow);

        return AddResult(result, utcNow);
    }

    /// <summary>Corrects a recorded result of either kind (FR-8.5).</summary>
    /// <param name="contractId">The contract whose result is corrected.</param>
    /// <param name="passed">Whether the student passed.</param>
    /// <param name="score">The score, from 0 to <see cref="MaxScore"/>, if given.</param>
    /// <param name="comment">An optional comment, for example why the result was corrected.</param>
    /// <param name="correctedByUserId">The user making the correction.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if there is no result for the contract or an argument is invalid.</returns>
    public Result CorrectResult(Guid contractId, bool passed, int? score, string? comment, Guid correctedByUserId, DateTime utcNow)
    {
        var result = _results.Find(candidate => candidate.ContractId == contractId);
        if (result is null)
            return Result.Failure(DomainErrors.Exam.ResultNotFound);

        var valuesResult = ValidateResultValues(score, comment, correctedByUserId);
        if (valuesResult.IsFailure)
            return valuesResult;

        result.Correct(passed, score, valuesResult.Value, correctedByUserId, utcNow);
        RaiseDomainEvent(new ExamResultRecordedDomainEvent(Id, result.ContractId, result.StudentId, Kind, passed, utcNow));

        return Result.Success();
    }

    private Result<ExamResult> AddResult(ExamResult result, DateTime utcNow)
    {
        _results.Add(result);
        RaiseDomainEvent(new ExamResultRecordedDomainEvent(Id, result.ContractId, result.StudentId, Kind, result.Passed, utcNow));

        return result;
    }

    private Result EnsureCanChangeSchedule()
    {
        if (Status != ExamStatus.Scheduled)
            return Result.Failure(DomainErrors.Exam.NotScheduled);

        if (_results.Count > 0)
            return Result.Failure(DomainErrors.Exam.HasResults);

        return Result.Success();
    }

    private Result EnsureCanRecord(StudyGroup group, Guid contractId, DateTime utcNow)
    {
        if (Status != ExamStatus.Scheduled)
            return Result.Failure(DomainErrors.Exam.NotScheduled);

        if (utcNow < ScheduledAtUtc)
            return Result.Failure(DomainErrors.Exam.NotStarted);

        if (group.Id != GroupId)
            return Result.Failure(DomainErrors.Exam.GroupMismatch);

        if (!group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.ContractId == contractId))
            return Result.Failure(DomainErrors.Exam.NotGroupMember);

        if (_results.Exists(result => result.ContractId == contractId))
            return Result.Failure(DomainErrors.Exam.AlreadyRecorded);

        return Result.Success();
    }

    private static Result<string?> ValidateResultValues(int? score, string? comment, Guid userId)
    {
        if (score is < 0 or > MaxScore)
            return Result.Failure<string?>(DomainErrors.Exam.ScoreOutOfRange);

        var userResult = Guard.NotEmpty(userId, DomainErrors.Exam.UserIdEmpty);
        if (userResult.IsFailure)
            return Result.Failure<string?>(userResult.Error);

        return Guard.OptionalText(comment, MaxCommentLength, DomainErrors.Exam.CommentTooLong);
    }

    private static Result ValidateTemplate(StudyGroup group, ExamKind kind, TestTemplate? testTemplate)
    {
        if (kind == ExamKind.Practice)
            return testTemplate is null ? Result.Success() : Result.Failure(DomainErrors.Exam.TemplateNotAllowed);

        if (testTemplate is null)
            return Result.Failure(DomainErrors.Exam.TemplateRequired);

        if (!testTemplate.IsActive)
            return Result.Failure(DomainErrors.Exam.TemplateInactive);

        if (testTemplate.Category != group.Category)
            return Result.Failure(DomainErrors.Exam.TemplateCategoryMismatch);

        return Result.Success();
    }
}

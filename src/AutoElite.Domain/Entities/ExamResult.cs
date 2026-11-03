namespace AutoElite.Domain.Entities;

/// <summary>The result of one student at an <see cref="Exam"/>. Part of the exam aggregate.</summary>
public sealed class ExamResult : BaseEntity
{
    /// <summary>Gets the identifier of the student's contract.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets a value indicating whether the student passed.</summary>
    public bool Passed { get; private set; }

    /// <summary>Gets the score: correct answers for a theory exam, points for a practical one, if given.</summary>
    public int? Score { get; private set; }

    /// <summary>Gets the examiner's comment, if any.</summary>
    public string? Comment { get; private set; }

    /// <summary>Gets the identifier of the test attempt the theory result was taken from, if any.</summary>
    public Guid? TestSessionId { get; private set; }

    /// <summary>Gets the identifier of the user who recorded the result, or <see langword="null"/> if it came from a test attempt.</summary>
    public Guid? RecordedByUserId { get; private set; }

    /// <summary>Gets the UTC time the result was recorded.</summary>
    public DateTime RecordedAtUtc { get; private set; }

    /// <summary>Gets the identifier of the user who last corrected the result, if it was corrected.</summary>
    public Guid? CorrectedByUserId { get; private set; }

    /// <summary>Gets the UTC time of the last correction, if any.</summary>
    public DateTime? CorrectedAtUtc { get; private set; }

    private ExamResult(Guid id, Guid contractId, Guid studentId) : base(id)
    {
        ContractId = contractId;
        StudentId = studentId;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private ExamResult()
    {
    }

    internal static ExamResult Record(
        Guid contractId, Guid studentId, bool passed, int? score, string? comment, Guid? testSessionId, Guid? recordedByUserId, DateTime utcNow) =>
        new(Guid.CreateVersion7(), contractId, studentId)
        {
            Passed = passed,
            Score = score,
            Comment = comment,
            TestSessionId = testSessionId,
            RecordedByUserId = recordedByUserId,
            RecordedAtUtc = utcNow
        };

    internal void Correct(bool passed, int? score, string? comment, Guid correctedByUserId, DateTime utcNow)
    {
        Passed = passed;
        Score = score;
        Comment = comment;
        CorrectedByUserId = correctedByUserId;
        CorrectedAtUtc = utcNow;
    }
}

namespace AutoElite.Domain.Entities;

/// <summary>
/// How a student answers one question of the bank when they practise outside tests: how many times they answered it,
/// how many of those were wrong, and whether the last answer was right. A question whose last answer was wrong is one
/// of the student's mistakes, which they practise again until they answer it right.
/// </summary>
/// <remarks>
/// A student has at most one record per question, which the persistence layer keeps unique; the first answer creates
/// it and the next ones update it.
/// </remarks>
public sealed class PracticeRecord : BaseEntity
{
    /// <summary>Gets the identifier of the student.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the identifier of the question of the bank.</summary>
    public Guid QuestionId { get; private set; }

    /// <summary>Gets how many times the student answered the question.</summary>
    public int AnswerCount { get; private set; }

    /// <summary>Gets how many of the answers were wrong.</summary>
    public int MistakeCount { get; private set; }

    /// <summary>Gets a value indicating whether the last answer was right.</summary>
    public bool IsLastAnswerCorrect { get; private set; }

    /// <summary>Gets the UTC time of the last answer.</summary>
    public DateTime LastAnsweredAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the question is one of the student's mistakes.</summary>
    public bool IsMistake => !IsLastAnswerCorrect;

    private PracticeRecord(Guid id, Guid studentId, Guid questionId) : base(id)
    {
        StudentId = studentId;
        QuestionId = questionId;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private PracticeRecord()
    {
    }

    /// <summary>Records the first answer of a student to a question.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="questionId">The question; checking that it exists and is active is up to the caller.</param>
    /// <param name="isCorrect">Whether the answer was right.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The record, or the first validation failure encountered.</returns>
    public static Result<PracticeRecord> Start(Guid studentId, Guid questionId, bool isCorrect, DateTime utcNow)
    {
        var studentResult = Guard.NotEmpty(studentId, DomainErrors.PracticeRecord.StudentIdEmpty);
        if (studentResult.IsFailure)
            return Result.Failure<PracticeRecord>(studentResult.Error);

        var questionResult = Guard.NotEmpty(questionId, DomainErrors.PracticeRecord.QuestionIdEmpty);
        if (questionResult.IsFailure)
            return Result.Failure<PracticeRecord>(questionResult.Error);

        var record = new PracticeRecord(Guid.CreateVersion7(), studentId, questionId);
        record.Record(isCorrect, utcNow);

        return record;
    }

    /// <summary>Records another answer to the question.</summary>
    /// <param name="isCorrect">Whether the answer was right.</param>
    /// <param name="utcNow">The current UTC time.</param>
    public void Record(bool isCorrect, DateTime utcNow)
    {
        AnswerCount++;
        if (!isCorrect)
            MistakeCount++;

        IsLastAnswerCorrect = isCorrect;
        LastAnsweredAtUtc = utcNow;
    }
}

namespace AutoElite.Domain.Entities;

/// <summary>The attendance mark of one student at a <see cref="TheoryLesson"/>. Part of the lesson aggregate.</summary>
public sealed class LessonAttendance : BaseEntity
{
    /// <summary>The longest comment of an attendance mark.</summary>
    public const int MaxCommentLength = 500;

    /// <summary>Gets the identifier of the student's contract.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student, copied from the contract.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets a value indicating whether the student attended.</summary>
    public bool IsPresent { get; private set; }

    /// <summary>Gets the comment, for example the reason for absence, if any.</summary>
    public string? Comment { get; private set; }

    /// <summary>Gets the identifier of the user who made or last corrected the mark.</summary>
    public Guid MarkedByUserId { get; private set; }

    /// <summary>Gets the UTC time the mark was made or last corrected.</summary>
    public DateTime MarkedAtUtc { get; private set; }

    private LessonAttendance(Guid id, Guid contractId, Guid studentId) : base(id)
    {
        ContractId = contractId;
        StudentId = studentId;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private LessonAttendance()
    {
    }

    /// <summary>Validates the comment and the author of a mark.</summary>
    internal static Result Validate(string? comment, Guid markedByUserId)
    {
        var commentResult = Guard.OptionalText(comment, MaxCommentLength, DomainErrors.LessonAttendance.CommentTooLong);
        if (commentResult.IsFailure)
            return Result.Failure(commentResult.Error);

        var authorResult = Guard.NotEmpty(markedByUserId, DomainErrors.LessonAttendance.MarkedByUserIdEmpty);
        if (authorResult.IsFailure)
            return Result.Failure(authorResult.Error);

        return Result.Success();
    }

    /// <summary>Creates a mark from values already checked by <see cref="Validate"/>.</summary>
    internal static LessonAttendance Create(
        Guid contractId, Guid studentId, bool isPresent, string? comment, Guid markedByUserId, DateTime utcNow)
    {
        var mark = new LessonAttendance(Guid.CreateVersion7(), contractId, studentId);
        mark.Correct(isPresent, comment, markedByUserId, utcNow);

        return mark;
    }

    /// <summary>Replaces the mark with values already checked by <see cref="Validate"/>.</summary>
    internal void Correct(bool isPresent, string? comment, Guid markedByUserId, DateTime utcNow)
    {
        IsPresent = isPresent;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        MarkedByUserId = markedByUserId;
        MarkedAtUtc = utcNow;
    }
}

using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A student's booking of a <see cref="DrivingSlot"/>, with the outcome of the lesson. Part of the slot aggregate;
/// a cancelled booking is kept as history.
/// </summary>
public sealed class DrivingBooking : BaseEntity
{
    private readonly List<SkillGrade> _skillGrades = [];

    /// <summary>Gets the identifier of the student's contract.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student, copied from the contract.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the UTC time the booking was made.</summary>
    public DateTime BookedAtUtc { get; private set; }

    /// <summary>Gets the booking status.</summary>
    public DrivingBookingStatus Status { get; private set; }

    /// <summary>Gets the UTC time the booking was cancelled, if it was.</summary>
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>Gets the identifier of the user who cancelled the booking, if it was cancelled.</summary>
    public Guid? CancelledByUserId { get; private set; }

    /// <summary>Gets the reason for the cancellation, if it was cancelled.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>Gets the actual length of the lesson in hours, once it has taken place.</summary>
    public decimal? ActualHours { get; private set; }

    /// <summary>Gets the instructor's grades per skill, once the lesson has taken place.</summary>
    public IReadOnlyList<SkillGrade> SkillGrades => _skillGrades.AsReadOnly();

    /// <summary>Gets the average of <see cref="SkillGrades"/> rounded to two decimals, or <see langword="null"/> if none were given.</summary>
    public decimal? AverageGrade => AverageOf(_skillGrades);

    /// <summary>Gets the instructor's note about the lesson, if any.</summary>
    public string? InstructorNote { get; private set; }

    /// <summary>Gets the UTC time the lesson was completed or the no-show was recorded.</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Gets the student's rating of the lesson from 1 to 5, if the student rated it.</summary>
    public int? StudentRating { get; private set; }

    /// <summary>Gets the student's comment on the lesson, if any.</summary>
    public string? StudentComment { get; private set; }

    /// <summary>Gets the UTC time the student rated the lesson, if they did.</summary>
    public DateTime? RatedAtUtc { get; private set; }

    private DrivingBooking(Guid id, Guid contractId, Guid studentId, DateTime bookedAtUtc) : base(id)
    {
        ContractId = contractId;
        StudentId = studentId;
        BookedAtUtc = bookedAtUtc;
        Status = DrivingBookingStatus.Active;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private DrivingBooking()
    {
    }

    /// <summary>Averages skill grades and rounds to two decimals; <see langword="null"/> for no grades.</summary>
    internal static decimal? AverageOf(IReadOnlyCollection<SkillGrade> skillGrades) =>
        skillGrades.Count == 0
            ? null
            : decimal.Round(skillGrades.Average(grade => (decimal)grade.Grade), 2, MidpointRounding.AwayFromZero);

    internal static DrivingBooking Create(Guid contractId, Guid studentId, DateTime utcNow) =>
        new(Guid.CreateVersion7(), contractId, studentId, utcNow);

    internal void Cancel(Guid? cancelledByUserId, string reason, DateTime utcNow)
    {
        Status = DrivingBookingStatus.Cancelled;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = reason;
        CancelledAtUtc = utcNow;
    }

    internal void Complete(decimal actualHours, IEnumerable<SkillGrade> skillGrades, string? instructorNote, DateTime utcNow)
    {
        Status = DrivingBookingStatus.Completed;
        ActualHours = actualHours;
        _skillGrades.AddRange(skillGrades);
        InstructorNote = instructorNote;
        CompletedAtUtc = utcNow;
    }

    internal void MarkNoShow(string? instructorNote, DateTime utcNow)
    {
        Status = DrivingBookingStatus.NoShow;
        InstructorNote = instructorNote;
        CompletedAtUtc = utcNow;
    }

    internal void Rate(int rating, string? comment, DateTime utcNow)
    {
        StudentRating = rating;
        StudentComment = comment;
        RatedAtUtc = utcNow;
    }
}

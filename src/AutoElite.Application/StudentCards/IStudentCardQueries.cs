namespace AutoElite.Application.StudentCards;

/// <summary>Reads student cards for queries.</summary>
public interface IStudentCardQueries
{
    /// <summary>Reads the card of a student with all of their contracts.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="utcNow">The current time, which divides held lessons from upcoming ones.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if no student has the identifier.</returns>
    Task<StudentCard?> GetAsync(Guid studentId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an instructor teaches a student: the student booked a slot of the driving instructor, the
    /// booking not cancelled, or is enrolled, not withdrawn, in an active group of the theory instructor.
    /// </summary>
    /// <param name="studentId">The student.</param>
    /// <param name="instructorId">The instructor.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> IsTaughtByAsync(Guid studentId, Guid instructorId, CancellationToken cancellationToken = default);
}

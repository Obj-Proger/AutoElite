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
}

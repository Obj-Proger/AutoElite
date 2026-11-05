namespace AutoElite.Application.Exams;

/// <summary>Loads and adds exams for commands.</summary>
public interface IExamRepository : IRepository<Exam>
{
    /// <summary>Loads the scheduled exams that take place after one UTC time and no later than another.</summary>
    /// <param name="afterUtc">Only exams later than this UTC time.</param>
    /// <param name="untilUtc">Only exams no later than this UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<Exam>> GetScheduledBetweenAsync(DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default);
}

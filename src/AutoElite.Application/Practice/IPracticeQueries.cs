namespace AutoElite.Application.Practice;

/// <summary>Reads the questions of the bank that students practise, and how far each student has gone through them.</summary>
public interface IPracticeQueries
{
    /// <summary>Counts the active questions of a licence category by topic, with the answers and mistakes of a student.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="category">The licence category of the questions.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The overview.</returns>
    Task<PracticeOverview> GetOverviewAsync(Guid studentId, LicenseCategory category, CancellationToken cancellationToken = default);

    /// <summary>Lists the active questions of a licence category to practise, ordered by their identifiers.</summary>
    /// <param name="studentId">The student, whose mistakes count when only those are asked for.</param>
    /// <param name="category">The licence category of the questions.</param>
    /// <param name="topicId">The topic of the questions, or <see langword="null"/> for every topic.</param>
    /// <param name="mistakesOnly">Whether only the questions the student last answered wrong are listed.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The identifiers of the questions.</returns>
    Task<IReadOnlyList<Guid>> GetQuestionIdsAsync(
        Guid studentId, LicenseCategory category, Guid? topicId, bool mistakesOnly, CancellationToken cancellationToken = default);

    /// <summary>Reads questions to practise, without their correct options.</summary>
    /// <param name="questionIds">The questions, in the order to give them.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The questions that exist, in the order asked for.</returns>
    Task<IReadOnlyList<PracticeQuestion>> GetQuestionsAsync(
        IReadOnlyList<Guid> questionIds, CancellationToken cancellationToken = default);
}

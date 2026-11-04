namespace AutoElite.Application.TestSessions;

/// <summary>What to draw from the question bank for a block of a test.</summary>
/// <param name="Category">The licence category the questions must apply to.</param>
/// <param name="Source">Only official or only school questions; <see langword="null"/> for both.</param>
/// <param name="TopicIds">The topics to draw from; empty for all topics.</param>
/// <param name="Count">How many questions to draw.</param>
/// <param name="ExcludedQuestionIds">Questions already shown in the attempt, which are not drawn again (FR-7.6).</param>
public sealed record QuestionPick(
    LicenseCategory Category,
    QuestionSource? Source,
    IReadOnlyCollection<Guid> TopicIds,
    int Count,
    IReadOnlyCollection<Guid> ExcludedQuestionIds);

/// <summary>Draws random questions from the bank for a test (FR-7.5).</summary>
public interface IQuestionPicker
{
    /// <summary>
    /// Draws up to <see cref="QuestionPick.Count"/> active questions that match, in random order, with their answer
    /// options; fewer if the bank does not have enough.
    /// </summary>
    /// <param name="pick">What to draw.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<Question>> PickAsync(QuestionPick pick, CancellationToken cancellationToken = default);
}

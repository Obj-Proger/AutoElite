namespace AutoElite.Mobile.Pages;

/// <summary>A question the student answered wrongly, shown after the attempt with the correct answer.</summary>
public sealed class MistakeEntry
{
    /// <summary>Gets the question.</summary>
    public required string Question { get; init; }

    /// <summary>Gets the answer the student chose, or that they did not answer.</summary>
    public required string YourAnswer { get; init; }

    /// <summary>Gets the correct answer.</summary>
    public required string CorrectAnswer { get; init; }

    /// <summary>Gets why the answer is correct, if the question explains it.</summary>
    public string? Explanation { get; init; }

    /// <summary>Gets a value indicating whether the question explains its answer.</summary>
    public bool HasExplanation => !string.IsNullOrWhiteSpace(Explanation);
}

namespace AutoElite.Domain.Enums;

/// <summary>Why a test attempt failed.</summary>
public enum TestFailureReason
{
    /// <summary>More mistakes in the main questions than the rules allow in total.</summary>
    TooManyMistakes = 1,

    /// <summary>More mistakes in the main questions of one block than the rules allow per block.</summary>
    TooManyMistakesInBlock = 2,

    /// <summary>A mistake in a question added after an earlier mistake.</summary>
    MistakeInAddOnQuestion = 3,

    /// <summary>The time ran out before every question was answered.</summary>
    TimeExpired = 4
}

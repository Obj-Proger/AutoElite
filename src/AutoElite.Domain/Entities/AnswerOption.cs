namespace AutoElite.Domain.Entities;

/// <summary>One answer option of a <see cref="Question"/>. Part of the question aggregate.</summary>
public sealed class AnswerOption : BaseEntity
{
    /// <summary>Gets the text of the option.</summary>
    public string Text { get; private set; } = null!;

    /// <summary>Gets the one-based position of the option as shown to the student.</summary>
    public int Position { get; private set; }

    /// <summary>Gets a value indicating whether this option is the correct answer.</summary>
    public bool IsCorrect { get; private set; }

    private AnswerOption(Guid id, string text, int position, bool isCorrect) : base(id)
    {
        Text = text;
        Position = position;
        IsCorrect = isCorrect;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private AnswerOption()
    {
    }

    /// <summary>Creates an option from values already checked by the question.</summary>
    internal static AnswerOption Create(string text, int position, bool isCorrect) =>
        new(Guid.CreateVersion7(), text, position, isCorrect);
}

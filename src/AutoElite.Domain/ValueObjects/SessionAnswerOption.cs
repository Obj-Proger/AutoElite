using AutoElite.Domain.Entities;

namespace AutoElite.Domain.ValueObjects;

/// <summary>A copy of one answer option of a question, as it was shown in a test session.</summary>
public sealed class SessionAnswerOption : ValueObject
{
    /// <summary>Gets the identifier of the option in the question bank.</summary>
    public Guid OptionId { get; }

    /// <summary>Gets the text of the option.</summary>
    public string Text { get; }

    private SessionAnswerOption(Guid optionId, string text)
    {
        OptionId = optionId;
        Text = text;
    }

    /// <summary>Copies an option of the bank.</summary>
    internal static SessionAnswerOption CopyOf(AnswerOption option) => new(option.Id, option.Text);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OptionId;
        yield return Text;
    }

    /// <inheritdoc />
    public override string ToString() => Text;
}

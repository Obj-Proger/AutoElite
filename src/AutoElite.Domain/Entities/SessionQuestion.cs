using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A copy of a bank question as it was shown in a <see cref="TestSession"/>, with the student's answer. Part of the
/// session aggregate. Because it is a copy, later edits of the bank never change a past attempt.
/// </summary>
public sealed class SessionQuestion : BaseEntity
{
    private readonly List<SessionAnswerOption> _options = [];

    /// <summary>Gets the identifier of the question in the bank.</summary>
    public Guid QuestionId { get; private set; }

    /// <summary>Gets the one-based position in the order the questions were shown.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the one-based number of the block the question belongs to.</summary>
    public int BlockNumber { get; private set; }

    /// <summary>Gets a value indicating whether the question was added after a mistake.</summary>
    public bool IsAddOn { get; private set; }

    /// <summary>Gets the question text.</summary>
    public string Text { get; private set; } = null!;

    /// <summary>Gets the location of the illustration in file storage, if any.</summary>
    public string? ImageKey { get; private set; }

    /// <summary>Gets the explanation of the correct answer, if any. Shown only after the attempt.</summary>
    public string? Explanation { get; private set; }

    /// <summary>Gets the answer options in the order they were shown.</summary>
    public IReadOnlyList<SessionAnswerOption> Options => _options.AsReadOnly();

    /// <summary>Gets the identifier of the correct option. Shown only after the attempt.</summary>
    public Guid CorrectOptionId { get; private set; }

    /// <summary>Gets the identifier of the option the student chose, or <see langword="null"/> if not yet answered.</summary>
    public Guid? SelectedOptionId { get; private set; }

    /// <summary>Gets the UTC time of the answer, if answered.</summary>
    public DateTime? AnsweredAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the question has been answered.</summary>
    public bool IsAnswered => SelectedOptionId is not null;

    /// <summary>Gets whether the answer was correct, or <see langword="null"/> if not yet answered.</summary>
    public bool? IsCorrect => SelectedOptionId is { } selected ? selected == CorrectOptionId : null;

    private SessionQuestion(Guid id, Question question, int position, int blockNumber, bool isAddOn) : base(id)
    {
        QuestionId = question.Id;
        Position = position;
        BlockNumber = blockNumber;
        IsAddOn = isAddOn;
        Text = question.Text;
        ImageKey = question.ImageKey;
        Explanation = question.Explanation;
        CorrectOptionId = question.CorrectOption.Id;
        _options.AddRange(question.Options.Select(SessionAnswerOption.CopyOf));
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private SessionQuestion()
    {
    }

    internal static SessionQuestion CopyOf(Question question, int position, int blockNumber, bool isAddOn) =>
        new(Guid.CreateVersion7(), question, position, blockNumber, isAddOn);

    internal bool HasOption(Guid optionId) => _options.Exists(option => option.OptionId == optionId);

    internal void Answer(Guid optionId, DateTime utcNow)
    {
        SelectedOptionId = optionId;
        AnsweredAtUtc = utcNow;
    }
}

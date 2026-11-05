namespace AutoElite.Domain.Entities;

/// <summary>A note written by a staff member about a <see cref="Lead"/>. Part of the lead aggregate.</summary>
public sealed class LeadNote : BaseEntity
{
    /// <summary>The longest note.</summary>
    public const int MaxTextLength = 4000;

    /// <summary>Gets the text of the note.</summary>
    public string Text { get; private set; } = null!;

    /// <summary>Gets the identifier of the user who wrote the note.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Gets the UTC time at which the note was written.</summary>
    public DateTime WrittenAtUtc { get; private set; }

    private LeadNote(Guid id, string text, Guid authorId, DateTime writtenAtUtc) : base(id)
    {
        Text = text;
        AuthorId = authorId;
        WrittenAtUtc = writtenAtUtc;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private LeadNote()
    {
    }

    /// <summary>Validates and creates a note. Called only by <see cref="Lead.AddNote"/>.</summary>
    internal static Result<LeadNote> Create(string? text, Guid authorId, DateTime utcNow)
    {
        var textResult = Guard.RequiredText(text, MaxTextLength, DomainErrors.LeadNote.TextEmpty, DomainErrors.LeadNote.TextTooLong);
        if (textResult.IsFailure)
            return Result.Failure<LeadNote>(textResult.Error);

        var authorResult = Guard.NotEmpty(authorId, DomainErrors.LeadNote.AuthorIdEmpty);
        if (authorResult.IsFailure)
            return Result.Failure<LeadNote>(authorResult.Error);

        return new LeadNote(Guid.CreateVersion7(), textResult.Value, authorId, utcNow);
    }
}

using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A traffic-rules question of the school's question bank: the text, an optional image, from 2 to 6 answer options of
/// which exactly one is correct, an explanation, the course topic and the licence categories it applies to.
/// </summary>
/// <remarks>
/// <para>
/// A test session keeps its own copy of every question it shows (text, options and the correct one), so editing a
/// question never changes past attempts. That is why <see cref="UpdateContent"/> may replace the options.
/// </para>
/// <para>
/// A question is taken out of use rather than deleted. Uniqueness of <see cref="ExternalCode"/>, used to recognize
/// already imported questions, is checked by the application layer.
/// </para>
/// </remarks>
public sealed class Question : AuditableEntity
{
    /// <summary>The fewest answer options a question may have.</summary>
    public const int MinOptions = 2;

    /// <summary>The most answer options a question may have.</summary>
    public const int MaxOptions = 6;

    /// <summary>The longest question text.</summary>
    public const int MaxTextLength = 2000;

    /// <summary>The longest explanation.</summary>
    public const int MaxExplanationLength = 4000;

    /// <summary>The longest text of an answer option.</summary>
    public const int MaxOptionTextLength = 1000;

    /// <summary>The longest external code.</summary>
    public const int MaxExternalCodeLength = 50;

    private readonly List<AnswerOption> _options = [];
    // Not read-only, so that the persistence layer can load the stored categories into it.
    private List<LicenseCategory> _categories = [];

    /// <summary>Gets the question text.</summary>
    public string Text { get; private set; } = null!;

    /// <summary>Gets the explanation of the correct answer, shown after the attempt, if any.</summary>
    public string? Explanation { get; private set; }

    /// <summary>
    /// Gets the identifier of the document with the illustration, if the question has one. Checking that the document
    /// is an image of this question is up to the caller.
    /// </summary>
    public Guid? ImageDocumentId { get; private set; }

    /// <summary>Gets the identifier of the course topic the question belongs to.</summary>
    public Guid TopicId { get; private set; }

    /// <summary>Gets where the question comes from.</summary>
    public QuestionSource Source { get; private set; }

    /// <summary>Gets the identifier of the question in its source, for example the ticket and question number, if any.</summary>
    public string? ExternalCode { get; private set; }

    /// <summary>Gets the licence categories the question applies to, in ascending order.</summary>
    public IReadOnlyList<LicenseCategory> Categories => _categories.AsReadOnly();

    /// <summary>Gets the answer options ordered by position.</summary>
    public IReadOnlyList<AnswerOption> Options => _options.AsReadOnly();

    /// <summary>Gets the correct answer option.</summary>
    public AnswerOption CorrectOption => _options.Single(option => option.IsCorrect);

    /// <summary>Gets a value indicating whether the question is used in new tests.</summary>
    public bool IsActive { get; private set; }

    private Question(Guid id, QuestionSource source) : base(id)
    {
        Source = source;
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Question()
    {
    }

    /// <summary>Adds a question to the bank.</summary>
    /// <param name="text">The question text. Required.</param>
    /// <param name="explanation">The explanation of the correct answer, if any.</param>
    /// <param name="topicId">The course topic; checking that it exists is up to the caller.</param>
    /// <param name="categories">The licence categories, at least one, without repetitions.</param>
    /// <param name="source">Where the question comes from.</param>
    /// <param name="externalCode">The identifier in the source, if any.</param>
    /// <param name="options">From <see cref="MinOptions"/> to <see cref="MaxOptions"/> options with different texts, exactly one correct.</param>
    /// <returns>The question, or the first validation failure encountered.</returns>
    public static Result<Question> Create(
        string? text,
        string? explanation,
        Guid topicId,
        IReadOnlyCollection<LicenseCategory> categories,
        QuestionSource source,
        string? externalCode,
        IReadOnlyList<AnswerOptionDraft> options)
    {
        var sourceResult = Guard.DefinedEnum(source, DomainErrors.Question.SourceNotDefined);
        if (sourceResult.IsFailure)
            return Result.Failure<Question>(sourceResult.Error);

        var question = new Question(Guid.CreateVersion7(), source);

        var contentResult = question.ApplyContent(text, explanation, topicId, categories, externalCode, options);
        if (contentResult.IsFailure)
            return Result.Failure<Question>(contentResult.Error);

        return question;
    }

    /// <summary>
    /// Replaces the content of the question, including its options. The source does not change.
    /// The rules are the same as in <see cref="Create"/>.
    /// </summary>
    /// <returns>A failure if the question is inactive, or the first validation failure encountered.</returns>
    public Result UpdateContent(
        string? text,
        string? explanation,
        Guid topicId,
        IReadOnlyCollection<LicenseCategory> categories,
        string? externalCode,
        IReadOnlyList<AnswerOptionDraft> options)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Question.CannotModifyInactive);

        return ApplyContent(text, explanation, topicId, categories, externalCode, options);
    }

    /// <summary>Makes a document the illustration of the question. Use <see cref="RemoveImage"/> to clear it.</summary>
    /// <param name="documentId">The document with the image.</param>
    /// <returns>A failure if the question is inactive or the identifier is empty.</returns>
    public Result SetImage(Guid documentId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Question.CannotModifyInactive);

        var documentResult = Guard.NotEmpty(documentId, DomainErrors.Question.ImageDocumentIdEmpty);
        if (documentResult.IsFailure)
            return Result.Failure(documentResult.Error);

        ImageDocumentId = documentId;

        return Result.Success();
    }

    /// <summary>Removes the illustration of the question; attempts already drawn keep it.</summary>
    /// <returns>A failure if the question has no illustration.</returns>
    public Result RemoveImage()
    {
        if (ImageDocumentId is null)
            return Result.Failure(DomainErrors.Question.ImageNotSet);

        ImageDocumentId = null;

        return Result.Success();
    }

    /// <summary>Takes the question out of use in new tests.</summary>
    /// <returns>A failure if the question is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Question.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Returns the question to use.</summary>
    /// <returns>A failure if the question is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.Question.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    /// <summary>Validates every value first and changes the question only if all of them are valid.</summary>
    private Result ApplyContent(
        string? text,
        string? explanation,
        Guid topicId,
        IReadOnlyCollection<LicenseCategory> categories,
        string? externalCode,
        IReadOnlyList<AnswerOptionDraft> options)
    {
        var textResult = Guard.RequiredText(text, MaxTextLength, DomainErrors.Question.TextEmpty, DomainErrors.Question.TextTooLong);
        if (textResult.IsFailure)
            return Result.Failure(textResult.Error);

        var explanationResult = Guard.OptionalText(explanation, MaxExplanationLength, DomainErrors.Question.ExplanationTooLong);
        if (explanationResult.IsFailure)
            return Result.Failure(explanationResult.Error);

        var topicResult = Guard.NotEmpty(topicId, DomainErrors.Question.TopicIdEmpty);
        if (topicResult.IsFailure)
            return Result.Failure(topicResult.Error);

        var categoriesResult = ValidateCategories(categories);
        if (categoriesResult.IsFailure)
            return categoriesResult;

        var codeResult = Guard.OptionalText(externalCode, MaxExternalCodeLength, DomainErrors.Question.ExternalCodeTooLong);
        if (codeResult.IsFailure)
            return Result.Failure(codeResult.Error);

        var optionsResult = ValidateOptions(options);
        if (optionsResult.IsFailure)
            return Result.Failure(optionsResult.Error);

        Text = textResult.Value;
        Explanation = explanationResult.Value;
        TopicId = topicId;
        ExternalCode = codeResult.Value;

        _categories.Clear();
        _categories.AddRange(categories.Order());

        _options.Clear();
        _options.AddRange(optionsResult.Value.Select((option, index) => AnswerOption.Create(option.Text, index + 1, option.IsCorrect)));

        return Result.Success();
    }

    private static Result ValidateCategories(IReadOnlyCollection<LicenseCategory> categories)
    {
        if (categories.Count == 0)
            return Result.Failure(DomainErrors.Question.NoCategories);

        if (categories.Any(category => !Enum.IsDefined(category)))
            return Result.Failure(DomainErrors.Question.CategoryNotDefined);

        if (categories.Distinct().Count() != categories.Count)
            return Result.Failure(DomainErrors.Question.DuplicateCategory);

        return Result.Success();
    }

    private static Result<List<(string Text, bool IsCorrect)>> ValidateOptions(IReadOnlyList<AnswerOptionDraft> options)
    {
        if (options.Count < MinOptions)
            return Result.Failure<List<(string, bool)>>(DomainErrors.Question.TooFewOptions);

        if (options.Count > MaxOptions)
            return Result.Failure<List<(string, bool)>>(DomainErrors.Question.TooManyOptions);

        var validated = new List<(string Text, bool IsCorrect)>(options.Count);

        foreach (var option in options)
        {
            var textResult = Guard.RequiredText(option.Text, MaxOptionTextLength, DomainErrors.Question.OptionTextEmpty, DomainErrors.Question.OptionTextTooLong);
            if (textResult.IsFailure)
                return Result.Failure<List<(string, bool)>>(textResult.Error);

            validated.Add((textResult.Value, option.IsCorrect));
        }

        if (validated.Select(option => option.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count() != validated.Count)
            return Result.Failure<List<(string, bool)>>(DomainErrors.Question.DuplicateOptionText);

        if (validated.Count(option => option.IsCorrect) != 1)
            return Result.Failure<List<(string, bool)>>(DomainErrors.Question.NotExactlyOneCorrectOption);

        return validated;
    }
}

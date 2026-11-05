using System.Globalization;
using AutoElite.Application.Documents;

namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>
/// Reads the file, checks every row as if the question were typed in, and adds or updates the questions only if all
/// rows are valid; then stores the illustrations of the rows.
/// </summary>
/// <remarks>
/// Rows are matched to the bank by external code, ignoring case. A matched question takes the content of the row and
/// moves to the topic of the import; it keeps its illustration unless the row brings one, which replaces it as
/// <see cref="SetQuestionImage.SetQuestionImageCommand"/> does, so attempts already drawn keep what they showed.
/// Images are stored only after every row is valid, and all of them are tried so that every image the storage rejects
/// is reported. Nothing is saved if the command fails; images stored by then are left without a record, and the
/// infrastructure sweeps such files away.
/// </remarks>
internal sealed class ImportQuestionsCommandHandler(
    IQuestionFileReader fileReader,
    IQuestionRepository questionRepository,
    QuestionAccess questionAccess,
    QuestionContent questionContent,
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ImportQuestionsCommand, QuestionImportResult>
{
    /// <summary>The most questions one file can have.</summary>
    internal const int MaxRows = 2000;

    /// <summary>The most row errors reported at once, so that a wrong file does not produce a huge response.</summary>
    internal const int MaxReportedErrors = 100;

    /// <inheritdoc />
    public async Task<Result<QuestionImportResult>> Handle(ImportQuestionsCommand command, CancellationToken cancellationToken)
    {
        if (!questionAccess.MayWrite(command.Source))
            return Result.Failure<QuestionImportResult>(ApplicationErrors.Question.OfficialAdministratorOnly);

        var topicResult = await questionContent.CheckTopicAsync(command.TopicId, cancellationToken);
        if (topicResult.IsFailure)
            return Result.Failure<QuestionImportResult>(topicResult.Error);

        var fileResult = await fileReader.ReadAsync(command.Content!, command.FileName!, cancellationToken);
        if (fileResult.IsFailure)
            return Result.Failure<QuestionImportResult>(fileResult.Error);

        await using var file = fileResult.Value;
        var rows = file.Rows;
        if (rows.Count == 0)
            return Result.Failure<QuestionImportResult>(ApplicationErrors.Question.ImportEmpty);

        if (rows.Count > MaxRows)
            return Result.Failure<QuestionImportResult>(ApplicationErrors.Question.ImportTooManyRows);

        var context = new ImportContext(
            command, file, RepeatedCodes(rows), await LoadExistingAsync(rows, cancellationToken),
            currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
        var errors = new List<FieldError>();
        var imported = new List<ImportedRow>();

        foreach (var row in rows)
        {
            var rowResult = ImportRow(row, context);
            if (rowResult.IsFailure)
                errors.Add(ErrorOf(row, rowResult.Error));
            else
                imported.Add(rowResult.Value);
        }

        if (errors.Count == 0)
            errors.AddRange(await StoreImagesAsync(file, imported, cancellationToken));

        if (errors.Count > 0)
        {
            var invalid = ApplicationErrors.Question.ImportInvalid;

            return Result.Failure<QuestionImportResult>(
                Error.Validation(invalid.Code, invalid.Message, [.. errors.Take(MaxReportedErrors)]));
        }

        foreach (var row in imported)
        {
            if (!row.Updated)
                questionRepository.Add(row.Question);

            if (row.Image is { } image)
                documentRepository.Add(image);
        }

        return new QuestionImportResult(
            imported.Count(row => !row.Updated), imported.Count(row => row.Updated), imported.Count(row => row.Image is not null));
    }

    /// <summary>Checks a row, adds or updates its question and prepares its illustration.</summary>
    /// <returns>The question of the row with its illustration to store, or the error of the row.</returns>
    private static Result<ImportedRow> ImportRow(QuestionImportRow row, ImportContext context)
    {
        var questionResult = ImportQuestion(row, context);
        if (questionResult.IsFailure)
            return Result.Failure<ImportedRow>(questionResult.Error);

        var (question, updated) = questionResult.Value;
        if (row.ImageName is null)
            return new ImportedRow(row, question, updated, null);

        var imageResult = PrepareImage(row.ImageName, question, context);
        if (imageResult.IsFailure)
            return Result.Failure<ImportedRow>(imageResult.Error);

        var setResult = question.SetImage(imageResult.Value.Id);

        return setResult.IsFailure
            ? Result.Failure<ImportedRow>(setResult.Error)
            : new ImportedRow(row, question, updated, imageResult.Value);
    }

    /// <summary>Checks a row and adds or updates its question.</summary>
    /// <returns>The question and whether it existed, or the error of the row.</returns>
    private static Result<(Question, bool)> ImportQuestion(QuestionImportRow row, ImportContext context)
    {
        var code = Normalize(row.ExternalCode);
        if (code is not null && context.RepeatedCodes.Contains(code))
            return Result.Failure<(Question, bool)>(ApplicationErrors.Question.ImportExternalCodeRepeated);

        var categoriesResult = ParseCategories(row.Categories);
        if (categoriesResult.IsFailure)
            return Result.Failure<(Question, bool)>(categoriesResult.Error);

        var command = context.Command;
        var drafts = QuestionContent.ToDrafts(row.Options);
        if (code is not null && context.Existing.TryGetValue(code, out var question))
        {
            if (question.Source != command.Source)
                return Result.Failure<(Question, bool)>(ApplicationErrors.Question.ImportSourceMismatch);

            var updateResult = question.UpdateContent(
                row.Text, row.Explanation, command.TopicId, categoriesResult.Value, code, drafts);

            return updateResult.IsFailure ? Result.Failure<(Question, bool)>(updateResult.Error) : (question, true);
        }

        var createResult = Question.Create(
            row.Text, row.Explanation, command.TopicId, categoriesResult.Value, command.Source, code, drafts);

        return createResult.IsFailure ? Result.Failure<(Question, bool)>(createResult.Error) : (createResult.Value, false);
    }

    /// <summary>Builds the document of an illustration; its file is stored later.</summary>
    private static Result<Document> PrepareImage(string imageName, Question question, ImportContext context)
    {
        if (context.File.GetImageSize(imageName) is not { } sizeBytes)
            return Result.Failure<Document>(ApplicationErrors.Question.ImportImageMissing);

        var fileName = imageName[(imageName.LastIndexOfAny(['/', '\\']) + 1)..];
        if (DocumentImages.ContentTypeOf(fileName) is not { } contentType)
            return Result.Failure<Document>(ApplicationErrors.Document.ImageRequired);

        return Document.Attach(
            EntityReference.Create(EntityKind.Question, question.Id).Value,
            fileName,
            contentType,
            sizeBytes,
            $"documents/{Guid.CreateVersion7():N}",
            null,
            context.UserId,
            context.UtcNow);
    }

    /// <summary>Stores the files of the illustrations.</summary>
    /// <returns>The errors of the rows whose image the storage rejected.</returns>
    private async Task<List<FieldError>> StoreImagesAsync(
        IQuestionImportFile file, List<ImportedRow> imported, CancellationToken cancellationToken)
    {
        var errors = new List<FieldError>();
        foreach (var row in imported)
        {
            if (row.Image is not { } image)
                continue;

            await using var content = await file.OpenImageAsync(row.Source.ImageName!, cancellationToken);
            var saveResult = await fileStorage.SaveAsync(
                image.StorageKey, content, image.ContentType, image.SizeBytes, cancellationToken);
            if (saveResult.IsFailure)
                errors.Add(ErrorOf(row.Source, saveResult.Error));
        }

        return errors;
    }

    private static Result<IReadOnlyCollection<LicenseCategory>> ParseCategories(IReadOnlyList<string> values)
    {
        var categories = new List<LicenseCategory>();
        foreach (var value in values)
        {
            var text = value.Trim();
            var isName = text.Length > 0 && !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            if (!isName || !Enum.TryParse<LicenseCategory>(text, ignoreCase: true, out var category) || !Enum.IsDefined(category))
                return Result.Failure<IReadOnlyCollection<LicenseCategory>>(ApplicationErrors.Question.ImportCategoryUnknown);

            categories.Add(category);
        }

        return categories;
    }

    private static HashSet<string> RepeatedCodes(IReadOnlyList<QuestionImportRow> rows) =>
        rows.Select(row => Normalize(row.ExternalCode))
            .OfType<string>()
            .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private async Task<Dictionary<string, Question>> LoadExistingAsync(
        IReadOnlyList<QuestionImportRow> rows, CancellationToken cancellationToken)
    {
        string[] codes = [.. rows.Select(row => Normalize(row.ExternalCode)).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];
        if (codes.Length == 0)
            return new Dictionary<string, Question>(StringComparer.OrdinalIgnoreCase);

        var questions = await questionRepository.GetByExternalCodesAsync(codes, cancellationToken);

        return questions.ToDictionary(question => question.ExternalCode!, StringComparer.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private static FieldError ErrorOf(QuestionImportRow row, Error error) =>
        new(string.Create(CultureInfo.InvariantCulture, $"rows[{row.RowNumber}]"), error.Code, error.Message);

    /// <summary>What every row of one import shares.</summary>
    private sealed record ImportContext(
        ImportQuestionsCommand Command,
        IQuestionImportFile File,
        HashSet<string> RepeatedCodes,
        Dictionary<string, Question> Existing,
        Guid UserId,
        DateTime UtcNow);

    /// <summary>A valid row: its question, whether the question existed, and the illustration to store, if any.</summary>
    private sealed record ImportedRow(QuestionImportRow Source, Question Question, bool Updated, Document? Image);
}

using System.Globalization;

namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>
/// Reads the file, checks every row as if the question were typed in, and adds or updates the questions only if all
/// rows are valid.
/// </summary>
/// <remarks>
/// Rows are matched to the bank by external code, ignoring case. A matched question takes the content of the row and
/// moves to the topic of the import; its illustration stays. Nothing is saved if the command fails, so the updates of
/// earlier rows are dropped together with the rest.
/// </remarks>
internal sealed class ImportQuestionsCommandHandler(
    IQuestionFileReader fileReader,
    IQuestionRepository questionRepository,
    QuestionAccess questionAccess,
    QuestionContent questionContent) : ICommandHandler<ImportQuestionsCommand, QuestionImportResult>
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

        var rowsResult = await fileReader.ReadAsync(command.Content!, command.FileName!, cancellationToken);
        if (rowsResult.IsFailure)
            return Result.Failure<QuestionImportResult>(rowsResult.Error);

        var rows = rowsResult.Value;
        if (rows.Count == 0)
            return Result.Failure<QuestionImportResult>(ApplicationErrors.Question.ImportEmpty);

        if (rows.Count > MaxRows)
            return Result.Failure<QuestionImportResult>(ApplicationErrors.Question.ImportTooManyRows);

        var errors = new List<FieldError>();
        var repeatedCodes = RepeatedCodes(rows);
        var existing = await LoadExistingAsync(rows, cancellationToken);
        var created = new List<Question>();
        var updated = 0;

        foreach (var row in rows)
        {
            var rowResult = ImportRow(row, command, repeatedCodes, existing, created);
            if (rowResult.IsFailure)
                errors.Add(new FieldError(FieldOf(row), rowResult.Error.Code, rowResult.Error.Message));
            else if (rowResult.Value)
                updated++;
        }

        if (errors.Count > 0)
        {
            var invalid = ApplicationErrors.Question.ImportInvalid;

            return Result.Failure<QuestionImportResult>(
                Error.Validation(invalid.Code, invalid.Message, [.. errors.Take(MaxReportedErrors)]));
        }

        foreach (var question in created)
            questionRepository.Add(question);

        return new QuestionImportResult(created.Count, updated);
    }

    /// <summary>Checks a row and adds or updates its question.</summary>
    /// <returns>Whether an existing question was updated, or the error of the row.</returns>
    private static Result<bool> ImportRow(
        QuestionImportRow row,
        ImportQuestionsCommand command,
        HashSet<string> repeatedCodes,
        Dictionary<string, Question> existing,
        List<Question> created)
    {
        var code = Normalize(row.ExternalCode);
        if (code is not null && repeatedCodes.Contains(code))
            return Result.Failure<bool>(ApplicationErrors.Question.ImportExternalCodeRepeated);

        var categoriesResult = ParseCategories(row.Categories);
        if (categoriesResult.IsFailure)
            return Result.Failure<bool>(categoriesResult.Error);

        var drafts = QuestionContent.ToDrafts(row.Options);
        if (code is not null && existing.TryGetValue(code, out var question))
        {
            if (question.Source != command.Source)
                return Result.Failure<bool>(ApplicationErrors.Question.ImportSourceMismatch);

            var updateResult = question.UpdateContent(
                row.Text, row.Explanation, question.ImageKey, command.TopicId, categoriesResult.Value, code, drafts);

            return updateResult.IsFailure ? Result.Failure<bool>(updateResult.Error) : true;
        }

        var createResult = Question.Create(
            row.Text, row.Explanation, null, command.TopicId, categoriesResult.Value, command.Source, code, drafts);
        if (createResult.IsFailure)
            return Result.Failure<bool>(createResult.Error);

        created.Add(createResult.Value);

        return false;
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

    private static string FieldOf(QuestionImportRow row) => string.Create(CultureInfo.InvariantCulture, $"rows[{row.RowNumber}]");
}

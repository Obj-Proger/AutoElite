using System.Globalization;
using System.Text.Json;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Infrastructure.Import;

/// <summary>Reads a JSON file of questions, telling the two formats apart by structure.</summary>
internal static class JsonQuestionFile
{
    private static readonly JsonDocumentOptions _options = new() { MaxDepth = ImportLimits.MaxJsonDepth };

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>Reads the questions: a list is <c>pdd_russia</c>, an object marked as ours is AutoElite.</summary>
    /// <param name="content">The content, in UTF-8 with or without a byte order mark.</param>
    /// <returns>The questions, or <see langword="null"/> if the JSON is in neither format.</returns>
    /// <exception cref="JsonException">The content is not JSON.</exception>
    /// <exception cref="InvalidDataException">A value has the wrong type.</exception>
    public static ParsedQuestionFile? Parse(byte[] content)
    {
        var json = content.AsMemory();
        if (json.Span.StartsWith(Utf8Bom))
            json = json[Utf8Bom.Length..];

        using var document = JsonDocument.Parse(json, _options);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return PddRussiaQuestions.Read(root);

        return AutoEliteQuestions.IsOwn(root) ? AutoEliteQuestions.Read(root) : null;
    }
}

/// <summary>
/// The JSON of the <c>etspring/pdd_russia</c> project, the de facto source of the official tickets: a list of questions
/// with <c>question</c>, <c>answers</c> (<c>answer_text</c>, <c>is_correct</c>), <c>answer_tip</c>, <c>image</c>,
/// <c>ticket_category</c> and <c>id</c>; the ticket, the title and the topics are not used.
/// </summary>
/// <remarks>
/// The tickets of <c>A,B</c> are those of categories A, A1, B, B1 and M, and the tickets of <c>C,D</c> those of C, C1, D
/// and D1, so a question gets all of them. The project names a question by a digest of its text and answers, and the
/// same question in both sets has a picture of its own in each, so the external code also names the set:
/// <c>pdd_russia:AB:{id}</c>. A question repeated in several tickets is read once.
/// </remarks>
internal static class PddRussiaQuestions
{
    /// <summary>The prefix of the external codes of the project's questions.</summary>
    public const string CodePrefix = "pdd_russia";

    // The placeholder the project gives a question without a picture.
    private const string NoImage = "images/no_image.jpg";

    private static readonly string[] _abmCategories = ["A", "A1", "B", "B1", "M"];

    private static readonly string[] _cdCategories = ["C", "C1", "D", "D1"];

    private static readonly char[] _categorySeparators = [',', ';', ' '];

    /// <summary>Reads the questions of the list.</summary>
    /// <param name="root">The list.</param>
    /// <exception cref="InvalidDataException">A value has the wrong type.</exception>
    public static ParsedQuestionFile Read(JsonElement root)
    {
        var rows = new List<QuestionImportRow>(root.GetArrayLength());
        foreach (var item in root.EnumerateArray())
        {
            var path = string.Create(CultureInfo.InvariantCulture, $"[{rows.Count}]");
            JsonFields.EnsureObject(item, path);
            var (set, categories) = SetOf(JsonFields.String(item, "ticket_category", path));
            var id = JsonFields.String(item, "id", path);
            var options = new List<AnswerOptionInput>();
            foreach (var (answer, answerPath) in JsonFields.Items(item, "answers", path))
            {
                JsonFields.EnsureObject(answer, answerPath);
                options.Add(new AnswerOptionInput(
                    JsonFields.String(answer, "answer_text", answerPath), JsonFields.Boolean(answer, "is_correct", answerPath)));
            }

            rows.Add(new QuestionImportRow(
                rows.Count + 1,
                JsonFields.String(item, "question", path),
                JsonFields.String(item, "answer_tip", path),
                categories,
                string.IsNullOrWhiteSpace(id) ? null : $"{CodePrefix}:{set}:{id.Trim()}",
                options,
                ImageOf(JsonFields.String(item, "image", path))));
        }

        return new ParsedQuestionFile(rows, RepeatsQuestions: true);
    }

    /// <summary>Gets the set of tickets and its categories; a set the project may add later keeps its own letters.</summary>
    private static (string Set, IReadOnlyList<string> Categories) SetOf(string? ticketCategory)
    {
        var letters = (ticketCategory ?? string.Empty).Split(
            _categorySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var set = string.Concat(letters);

        return set.ToUpperInvariant() switch
        {
            "AB" => ("AB", _abmCategories),
            "CD" => ("CD", _cdCategories),
            _ => (set, letters)
        };
    }

    private static string? ImageOf(string? image) =>
        string.IsNullOrWhiteSpace(image)
        || string.Equals(ImportPaths.Normalize(image.Trim()), NoImage, StringComparison.OrdinalIgnoreCase)
            ? null
            : image.Trim();
}

/// <summary>
/// The JSON of AutoElite, versioned so that a later export can be imported without losses:
/// <c>{"format": "autoelite.questions", "version": 1, "questions": [...]}</c>, each question with <c>code</c>,
/// <c>text</c>, <c>explanation</c>, <c>categories</c> (a list), <c>image</c> and <c>options</c> (<c>text</c>,
/// <c>isCorrect</c>). Other properties are ignored, and a version the reader does not know is a format it does not read.
/// </summary>
internal static class AutoEliteQuestions
{
    /// <summary>The value of <c>format</c> that marks the file.</summary>
    public const string FormatName = "autoelite.questions";

    /// <summary>The version of the format that the reader reads.</summary>
    public const int Version = 1;

    /// <summary>Determines whether the JSON is marked as a file of AutoElite.</summary>
    /// <param name="root">The root of the JSON.</param>
    public static bool IsOwn(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("format", out var format)
        && format.ValueKind == JsonValueKind.String
        && format.ValueEquals(FormatName);

    /// <summary>Reads the questions of the file.</summary>
    /// <param name="root">The root of the file.</param>
    /// <returns>The questions, or <see langword="null"/> for a version the reader does not know.</returns>
    /// <exception cref="InvalidDataException">A value has the wrong type.</exception>
    public static ParsedQuestionFile? Read(JsonElement root)
    {
        if (!root.TryGetProperty("version", out var version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var number))
        {
            throw new InvalidDataException("version must be a whole number.");
        }

        if (number != Version)
            return null;

        var rows = new List<QuestionImportRow>();
        foreach (var (item, path) in JsonFields.Items(root, "questions", string.Empty, required: true))
        {
            JsonFields.EnsureObject(item, path);
            var categories = new List<string>();
            foreach (var (category, categoryPath) in JsonFields.Items(item, "categories", path))
                categories.Add(JsonFields.StringValue(category, categoryPath) ?? string.Empty);

            var options = new List<AnswerOptionInput>();
            foreach (var (option, optionPath) in JsonFields.Items(item, "options", path))
            {
                JsonFields.EnsureObject(option, optionPath);
                options.Add(new AnswerOptionInput(
                    JsonFields.String(option, "text", optionPath), JsonFields.Boolean(option, "isCorrect", optionPath)));
            }

            var image = JsonFields.String(item, "image", path);
            rows.Add(new QuestionImportRow(
                rows.Count + 1,
                JsonFields.String(item, "text", path),
                JsonFields.String(item, "explanation", path),
                categories,
                JsonFields.String(item, "code", path),
                options,
                string.IsNullOrWhiteSpace(image) ? null : image.Trim()));
        }

        return new ParsedQuestionFile(rows, RepeatsQuestions: false);
    }
}

/// <summary>Reads the values of a JSON object, reporting a value of the wrong type with its path.</summary>
internal static class JsonFields
{
    /// <summary>Checks that a value is an object.</summary>
    /// <exception cref="InvalidDataException">It is not.</exception>
    public static void EnsureObject(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw Wrong(path, "an object");
    }

    /// <summary>Gets a text property; a missing or <c>null</c> one is <see langword="null"/>.</summary>
    /// <exception cref="InvalidDataException">The value is not text.</exception>
    public static string? String(JsonElement item, string name, string path) =>
        item.TryGetProperty(name, out var value) ? StringValue(value, $"{path}.{name}") : null;

    /// <summary>Gets a text value; <c>null</c> is <see langword="null"/>.</summary>
    /// <exception cref="InvalidDataException">The value is not text.</exception>
    public static string? StringValue(JsonElement value, string path) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => throw Wrong(path, "text")
        };

    /// <summary>Gets a property that is true or false; a missing or <c>null</c> one is false.</summary>
    /// <exception cref="InvalidDataException">The value is not true or false.</exception>
    public static bool Boolean(JsonElement item, string name, string path)
    {
        if (!item.TryGetProperty(name, out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False or JsonValueKind.Null => false,
            _ => throw Wrong($"{path}.{name}", "true or false")
        };
    }

    /// <summary>Gets the items of a list property with their paths; a missing or <c>null</c> one has none.</summary>
    /// <param name="item">The object.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="path">The path of the object, empty for the root.</param>
    /// <param name="required">Whether a missing list is an error.</param>
    /// <exception cref="InvalidDataException">The value is not a list, or a required one is missing.</exception>
    public static IReadOnlyList<(JsonElement Item, string Path)> Items(
        JsonElement item, string name, string path, bool required = false)
    {
        var listPath = path.Length == 0 ? name : $"{path}.{name}";
        var missing = !item.TryGetProperty(name, out var list) || list.ValueKind == JsonValueKind.Null;
        if (missing && required)
            throw Wrong(listPath, "a list");

        if (missing)
            return [];

        if (list.ValueKind != JsonValueKind.Array)
            throw Wrong(listPath, "a list");

        return list.EnumerateArray()
            .Select((value, index) => (Item: value, Path: string.Create(CultureInfo.InvariantCulture, $"{listPath}[{index}]")))
            .ToList();
    }

    private static InvalidDataException Wrong(string path, string expected) => new($"{path} must be {expected}.");
}

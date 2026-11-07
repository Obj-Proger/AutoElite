using System.Globalization;
using System.Text.RegularExpressions;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Infrastructure.Import;

/// <summary>A row of a table: its number in the file, from 1 for the first row, and its cells.</summary>
/// <param name="Number">The number of the row, as a spreadsheet shows it.</param>
/// <param name="Cells">The cells, from the first column.</param>
internal sealed record TableRow(int Number, IReadOnlyList<string> Cells);

/// <summary>
/// Reads the questions of a table, a CSV file or a worksheet: the first row names the columns, and every other row
/// that is not empty is a question numbered as the spreadsheet shows it. The columns are told by name, in English or
/// Russian, ignoring case, spaces and punctuation: <c>question</c>, <c>correct</c> (the number of the correct option)
/// and <c>option1</c>, <c>option2</c> and so on are required; <c>code</c>, <c>explanation</c>, <c>categories</c>
/// (separated by commas, semicolons, slashes or spaces) and <c>image</c> are optional; other columns are ignored.
/// </summary>
internal static partial class QuestionTable
{
    private const string RequiredColumns =
        "The first row must name the columns question, correct and option1, option2 and so on.";

    private static readonly Dictionary<string, Column> _columns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = Column.Code,
        ["externalcode"] = Column.Code,
        ["код"] = Column.Code,
        ["внешнийкод"] = Column.Code,
        ["question"] = Column.Text,
        ["text"] = Column.Text,
        ["вопрос"] = Column.Text,
        ["текствопроса"] = Column.Text,
        ["explanation"] = Column.Explanation,
        ["пояснение"] = Column.Explanation,
        ["categories"] = Column.Categories,
        ["category"] = Column.Categories,
        ["категории"] = Column.Categories,
        ["категория"] = Column.Categories,
        ["image"] = Column.Image,
        ["picture"] = Column.Image,
        ["изображение"] = Column.Image,
        ["картинка"] = Column.Image,
        ["correct"] = Column.Correct,
        ["correctoption"] = Column.Correct,
        ["correctanswer"] = Column.Correct,
        ["правильный"] = Column.Correct,
        ["правильныйответ"] = Column.Correct,
        ["правильныйвариант"] = Column.Correct
    };

    private static readonly char[] _categorySeparators = [',', ';', '/', ' ', '\t'];

    private enum Column
    {
        Code,
        Text,
        Explanation,
        Categories,
        Image,
        Correct
    }

    /// <summary>Reads the questions of the table.</summary>
    /// <param name="table">The rows of the table, the first naming the columns.</param>
    /// <exception cref="InvalidDataException">The first row lacks a required column or names one twice.</exception>
    public static List<QuestionImportRow> Read(IReadOnlyList<TableRow> table)
    {
        if (table.Count == 0)
            throw new InvalidDataException(RequiredColumns);

        var layout = Layout.Of(table[0].Cells);
        var rows = new List<QuestionImportRow>();
        foreach (var row in table.Skip(1))
        {
            if (!row.Cells.All(string.IsNullOrWhiteSpace))
                rows.Add(layout.ToRow(row));
        }

        return rows;
    }

    // The name of an option column, such as "option1", "answer 2" or "вариант 3", once spaces are removed.
    [GeneratedRegex("^(?:option|answer|вариант|ответ)([0-9]{1,2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OptionColumn();

    /// <summary>The name of a column without spaces and punctuation, so that "Option 1" and "option_1" are the same.</summary>
    private static string Normalize(string name) => string.Concat(name.Where(char.IsLetterOrDigit));

    /// <summary>Which column holds what.</summary>
    /// <param name="columns">The index of each named column.</param>
    /// <param name="options">The index of each option column, by the number of the option.</param>
    private sealed class Layout(Dictionary<Column, int> columns, SortedDictionary<int, int> options)
    {
        public static Layout Of(IReadOnlyList<string> header)
        {
            var columns = new Dictionary<Column, int>();
            var options = new SortedDictionary<int, int>();
            for (var index = 0; index < header.Count; index++)
            {
                var name = Normalize(header[index]);
                var repeated = false;
                if (OptionColumn().Match(name) is { Success: true } match)
                {
                    var number = int.Parse(match.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture);
                    repeated = number > 0 && !options.TryAdd(number, index);
                }
                else if (_columns.TryGetValue(name, out var column))
                {
                    repeated = !columns.TryAdd(column, index);
                }

                if (repeated)
                    throw new InvalidDataException($"The column \"{header[index].Trim()}\" is named twice.");
            }

            if (!columns.ContainsKey(Column.Text) || !columns.ContainsKey(Column.Correct) || options.Count == 0)
                throw new InvalidDataException(RequiredColumns);

            return new Layout(columns, options);
        }

        public QuestionImportRow ToRow(TableRow row)
        {
            var correct = int.TryParse(
                Cell(row, Column.Correct)?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0;
            var answerOptions = new List<AnswerOptionInput>();
            foreach (var (optionNumber, index) in options)
            {
                if (CellAt(row, index) is { } text)
                    answerOptions.Add(new AnswerOptionInput(text, optionNumber == correct));
            }

            var categories = Cell(row, Column.Categories)?.Split(
                _categorySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new QuestionImportRow(
                row.Number,
                Cell(row, Column.Text),
                Cell(row, Column.Explanation),
                categories ?? [],
                Cell(row, Column.Code),
                answerOptions,
                Cell(row, Column.Image)?.Trim());
        }

        private string? Cell(TableRow row, Column column) =>
            columns.TryGetValue(column, out var index) ? CellAt(row, index) : null;

        private static string? CellAt(TableRow row, int index) =>
            index < row.Cells.Count && !string.IsNullOrWhiteSpace(row.Cells[index]) ? row.Cells[index] : null;
    }
}

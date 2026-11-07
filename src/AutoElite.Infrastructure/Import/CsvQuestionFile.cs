using System.Text;

namespace AutoElite.Infrastructure.Import;

/// <summary>
/// Reads a CSV table of questions (<see cref="QuestionTable"/>) as RFC 4180 describes it and as Excel saves it: in UTF-8,
/// with or without a byte order mark, or in Windows-1251, which a Russian Excel uses for "CSV (comma delimited)", or in
/// UTF-16 with a byte order mark; with fields separated by commas, semicolons, which a Russian Excel uses, or tabs,
/// whichever the first row has most of. A field in double quotes may hold separators, line breaks and doubled quotes.
/// </summary>
internal static class CsvQuestionFile
{
    private const int Windows1251 = 1251;

    // In order of preference when the first row has as many of each.
    private static readonly char[] _separators = [';', ',', '\t'];

    private static readonly UTF8Encoding _utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private static ReadOnlySpan<byte> Utf16LittleEndianBom => [0xFF, 0xFE];

    private static ReadOnlySpan<byte> Utf16BigEndianBom => [0xFE, 0xFF];

    /// <summary>Reads the questions of the file.</summary>
    /// <param name="content">The content.</param>
    /// <exception cref="InvalidDataException">The content is not text, a quote is not closed, or a column is missing.</exception>
    public static ParsedQuestionFile Parse(byte[] content)
    {
        var text = Decode(content);
        if (text.Contains('\0', StringComparison.Ordinal))
            throw new InvalidDataException("The file is not text.");

        return new ParsedQuestionFile(QuestionTable.Read(Records(text, SeparatorOf(text))), RepeatsQuestions: false);
    }

    /// <summary>Decodes the text by its byte order mark; text without one is UTF-8 if it is valid UTF-8, else Windows-1251.</summary>
    /// <exception cref="InvalidDataException">The text is not valid in the encoding its byte order mark names.</exception>
    internal static string Decode(byte[] content)
    {
        ReadOnlySpan<byte> bytes = content;
        if (bytes.StartsWith(Utf8Bom))
            return Strict(_utf8, bytes[Utf8Bom.Length..]);

        if (bytes.StartsWith(Utf16LittleEndianBom))
            return Strict(new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true), bytes[2..]);

        if (bytes.StartsWith(Utf16BigEndianBom))
            return Strict(new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true), bytes[2..]);

        try
        {
            return _utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // Every byte is a character in Windows-1251, so this always decodes.
            var windows1251 = CodePagesEncodingProvider.Instance.GetEncoding(Windows1251)
                ?? throw new InvalidOperationException("The Windows-1251 encoding is not available.");

            return windows1251.GetString(bytes);
        }
    }

    /// <summary>Chooses the separator the first row has most of, outside quotes; a single column is comma-separated.</summary>
    internal static char SeparatorOf(string text)
    {
        Span<int> counts = stackalloc int[_separators.Length];
        var inQuotes = false;
        foreach (var character in text)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                if (character is '\r' or '\n')
                    break;

                var index = Array.IndexOf(_separators, character);
                if (index >= 0)
                    counts[index]++;
            }
        }

        var best = 0;
        for (var index = 1; index < counts.Length; index++)
        {
            if (counts[index] > counts[best])
                best = index;
        }

        return counts[best] > 0 ? _separators[best] : ',';
    }

    /// <summary>Splits the text into records numbered from 1; an empty line is a record with one empty field.</summary>
    /// <exception cref="InvalidDataException">A quoted field is not closed.</exception>
    internal static List<TableRow> Records(string text, char separator)
    {
        var records = new List<TableRow>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quoted = false;

        void EndField()
        {
            fields.Add(field.ToString());
            field.Clear();
            quoted = false;
        }

        void EndRecord()
        {
            EndField();
            records.Add(new TableRow(records.Count + 1, [.. fields]));
            fields.Clear();
        }

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (inQuotes)
            {
                if (character != '"')
                    field.Append(character);
                else if (index + 1 < text.Length && text[index + 1] == '"')
                    field.Append(text[++index]);
                else
                    inQuotes = false;
            }
            else if (character == '"' && field.Length == 0 && !quoted)
            {
                inQuotes = quoted = true;
            }
            else if (character == separator)
            {
                EndField();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;

                EndRecord();
            }
            else
            {
                // A quote inside an unquoted field, or text after a closing quote, is kept as Excel keeps it.
                field.Append(character);
            }
        }

        if (inQuotes)
            throw new InvalidDataException("A field in quotes is not closed.");

        if (field.Length > 0 || fields.Count > 0 || quoted)
            EndRecord();

        return records;
    }

    private static string Strict(Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        try
        {
            return encoding.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("The file is not valid text in the encoding its first bytes name.", exception);
        }
    }
}

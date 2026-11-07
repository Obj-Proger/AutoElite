using System.Globalization;
using System.Text.Json;
using AutoElite.Application.Common;
using AutoElite.Application.Questions.ImportQuestions;

namespace AutoElite.Infrastructure.Import;

/// <summary>The formats of a file of questions and of the archive that packs them with images.</summary>
internal enum QuestionFileKind
{
    /// <summary>JSON of <c>pdd_russia</c> or of AutoElite, told apart by structure.</summary>
    Json,

    /// <summary>A table in CSV.</summary>
    Csv,

    /// <summary>A table in an Excel workbook.</summary>
    Xlsx,

    /// <summary>A ZIP archive of files of questions and images.</summary>
    Zip
}

/// <summary>
/// Reads the files of a question import (D-070): JSON of the <c>etspring/pdd_russia</c> project, JSON of AutoElite, CSV
/// and Excel tables, alone or in a ZIP archive with the images they name. The extension tells the format and the content
/// must match it; the two JSON formats are told apart by structure. Every limit of <see cref="ImportLimits"/> applies,
/// since the file comes from a user.
/// </summary>
/// <remarks>
/// A file that cannot be read gives <see cref="ApplicationErrors.Question.ImportUnreadable"/>, and content in no format
/// the import reads gives <see cref="ApplicationErrors.Question.ImportFormatNotSupported"/>; both carry the reason as a
/// field error of <see cref="FileField"/>, or of <c>file[path]</c> for a file in the archive.
/// </remarks>
internal sealed class QuestionFileReader : IQuestionFileReader
{
    /// <summary>The field of the errors of the uploaded file.</summary>
    public const string FileField = "file";

    private const int CopyBufferSize = 81920;

    private const string UnknownContent =
        "The content is in no format the import reads: a JSON file must be a list of pdd_russia questions or an AutoElite "
        + "file of a version the import knows.";

    private static readonly Dictionary<string, QuestionFileKind> _kindByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".json"] = QuestionFileKind.Json,
        [".csv"] = QuestionFileKind.Csv,
        [".xlsx"] = QuestionFileKind.Xlsx,
        [".zip"] = QuestionFileKind.Zip
    };

    /// <summary>Gets the format of a file by its extension.</summary>
    /// <param name="fileName">The name or path of the file.</param>
    /// <returns>The format, or <see langword="null"/> if the import reads no file with the extension.</returns>
    public static QuestionFileKind? KindOf(string fileName) =>
        _kindByExtension.TryGetValue(Path.GetExtension(fileName), out var kind) ? kind : null;

    /// <inheritdoc />
    public async Task<Result<IQuestionImportFile>> ReadAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fileName);
        if (KindOf(fileName) is not { } kind)
            return Result.Failure<IQuestionImportFile>(ApplicationErrors.Question.ImportFormatNotSupported);

        if (kind == QuestionFileKind.Zip)
            return await ReadArchiveAsync(content, cancellationToken);

        ParsedQuestionFile? file;
        try
        {
            using var copy = new MemoryStream();
            await CopyAsync(content, copy, ImportLimits.MaxQuestionFileBytes, cancellationToken);
            file = Parse(kind, copy.ToArray());
        }
        catch (Exception exception) when (IsBadContent(exception))
        {
            return Result.Failure<IQuestionImportFile>(Unreadable(FileField, exception.Message));
        }

        return file is null
            ? Result.Failure<IQuestionImportFile>(NotSupported(FileField))
            : Result.Success<IQuestionImportFile>(new QuestionImportFile(QuestionRows.Combine([file]), null));
    }

    private static async Task<Result<IQuestionImportFile>> ReadArchiveAsync(Stream content, CancellationToken cancellationToken)
    {
        var spooled = CreateTemporaryFile();
        ImportArchive archive;
        try
        {
            await CopyAsync(content, spooled, ImportLimits.MaxArchiveBytes, cancellationToken);
            spooled.Position = 0;
            archive = ImportArchive.Open(spooled);
        }
        catch (Exception exception) when (IsBadContent(exception))
        {
            await spooled.DisposeAsync();

            return Result.Failure<IQuestionImportFile>(Unreadable(FileField, exception.Message));
        }
        catch
        {
            await spooled.DisposeAsync();
            throw;
        }

        try
        {
            var rowsResult = await ReadQuestionFilesAsync(archive, cancellationToken);
            if (rowsResult.IsFailure)
            {
                archive.Dispose();

                return Result.Failure<IQuestionImportFile>(rowsResult.Error);
            }

            return Result.Success<IQuestionImportFile>(new QuestionImportFile(rowsResult.Value, archive));
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    private static async Task<Result<IReadOnlyList<QuestionImportRow>>> ReadQuestionFilesAsync(
        ImportArchive archive, CancellationToken cancellationToken)
    {
        var entries = archive.QuestionFiles;
        if (entries.Count == 0)
        {
            return Result.Failure<IReadOnlyList<QuestionImportRow>>(
                Unreadable(FileField, "The archive has no file of questions (.json, .csv or .xlsx)."));
        }

        if (entries.Count > ImportLimits.MaxQuestionFiles)
        {
            return Result.Failure<IReadOnlyList<QuestionImportRow>>(Unreadable(
                FileField,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The archive has more than {ImportLimits.MaxQuestionFiles} files of questions.")));
        }

        var files = new List<ParsedQuestionFile>(entries.Count);
        foreach (var (path, entry) in entries)
        {
            var field = $"{FileField}[{path}]";
            ParsedQuestionFile? file;
            try
            {
                file = Parse(KindOf(path)!.Value, await ImportArchive.ReadAllAsync(entry, cancellationToken));
            }
            catch (Exception exception) when (IsBadContent(exception))
            {
                return Result.Failure<IReadOnlyList<QuestionImportRow>>(Unreadable(field, exception.Message));
            }

            if (file is null)
                return Result.Failure<IReadOnlyList<QuestionImportRow>>(NotSupported(field));

            files.Add(file);
        }

        return Result.Success(QuestionRows.Combine(files));
    }

    /// <summary>Reads a file of questions by its format.</summary>
    /// <returns>The questions, or <see langword="null"/> if the content is in no format the import reads.</returns>
    private static ParsedQuestionFile? Parse(QuestionFileKind kind, byte[] content) =>
        kind switch
        {
            QuestionFileKind.Json => JsonQuestionFile.Parse(content),
            QuestionFileKind.Csv => CsvQuestionFile.Parse(content),
            QuestionFileKind.Xlsx => XlsxQuestionFile.Parse(content),
            _ => null
        };

    /// <summary>Copies the content, stopping as soon as it is larger than allowed.</summary>
    /// <exception cref="InvalidDataException">The content is larger than allowed.</exception>
    private static async Task CopyAsync(Stream source, Stream target, long maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[CopyBufferSize];
        long copied = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            copied += read;
            if (copied > maxBytes)
                throw new InvalidDataException($"The file is larger than {ImportLimits.Megabytes(maxBytes)}.");

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    /// <summary>Creates a temporary file that only this process can read and that is deleted when it is closed.</summary>
    private static FileStream CreateTemporaryFile()
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.DeleteOnClose
        };

        // Windows has no Unix permissions, and setting them at all throws there.
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        return new FileStream(Path.Combine(Path.GetTempPath(), $"autoelite-import-{Guid.NewGuid():N}.zip"), options);
    }

    /// <summary>Determines whether an exception reports content that cannot be read rather than a failure of the server.</summary>
    private static bool IsBadContent(Exception exception) => exception is InvalidDataException or JsonException;

    private static Error Unreadable(string field, string reason) => WithReason(ApplicationErrors.Question.ImportUnreadable, field, reason);

    private static Error NotSupported(string field) =>
        WithReason(ApplicationErrors.Question.ImportFormatNotSupported, field, UnknownContent);

    private static Error WithReason(Error error, string field, string reason) =>
        Error.Validation(error.Code, error.Message, [new FieldError(field, error.Code, reason)]);
}

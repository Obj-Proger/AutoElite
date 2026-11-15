using System.Globalization;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>The size of a file as the user reads it: in bytes, kilobytes or megabytes.</summary>
internal static class FileSizes
{
    /// <summary>Gives the size of a file.</summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>The size, in the language of the application.</returns>
    public static string Of(long bytes) => bytes switch
    {
        < 1024 => AppTexts.Format("Files.Bytes", bytes.ToString(CultureInfo.CurrentCulture)),
        < 1024 * 1024 => AppTexts.Format("Files.Kilobytes", (bytes / 1024d).ToString("0.#", CultureInfo.CurrentCulture)),
        _ => AppTexts.Format("Files.Megabytes", (bytes / (1024d * 1024)).ToString("0.#", CultureInfo.CurrentCulture))
    };
}

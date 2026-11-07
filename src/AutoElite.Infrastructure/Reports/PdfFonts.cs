using System.Globalization;
using System.Text;
using SkiaSharp;

namespace AutoElite.Infrastructure.Reports;

/// <summary>The styles of the report font.</summary>
internal enum PdfFontStyle
{
    /// <summary>The regular style.</summary>
    Regular,

    /// <summary>The bold style.</summary>
    Bold
}

/// <summary>
/// The fonts of the PDF reports: PT Sans by ParaType, under the SIL Open Font License (see <c>Reports/Fonts/OFL.txt</c>),
/// embedded in the assembly so that the server needs no fonts of its own and Cyrillic always renders. The typefaces are
/// loaded once and shared; the sized fonts of one rendering are not shared, since they are not thread-safe.
/// </summary>
internal sealed class PdfFonts : IDisposable
{
    private const string ResourcePrefix = "Fonts/";

    private static readonly Lazy<SKTypeface> _regular = new(() => Load("PT_Sans-Web-Regular.ttf"));

    private static readonly Lazy<SKTypeface> _bold = new(() => Load("PT_Sans-Web-Bold.ttf"));

    private readonly Dictionary<(PdfFontStyle Style, float Size), SKFont> _fonts = new();

    /// <summary>Gets the font of a style and size.</summary>
    /// <param name="style">The style.</param>
    /// <param name="size">The size in points.</param>
    public SKFont Get(PdfFontStyle style, float size)
    {
        if (!_fonts.TryGetValue((style, size), out var font))
        {
            font = new SKFont(Typeface(style), size) { Subpixel = true, LinearMetrics = true };
            _fonts.Add((style, size), font);
        }

        return font;
    }

    /// <summary>
    /// Prepares text for the font: a space the font has no glyph for, such as the narrow no-break space some cultures
    /// group digits with, becomes a no-break space, and control characters become spaces, so that neither prints as a
    /// missing glyph.
    /// </summary>
    /// <param name="text">The text, without line breaks.</param>
    /// <param name="font">The font it is written in.</param>
    public static string Printable(string text, SKFont font)
    {
        var printable = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.Control)
                printable.Append(' ');
            else if (category == UnicodeCategory.SpaceSeparator && !font.ContainsGlyph(rune.Value))
                printable.Append('\u00A0');
            else
                printable.Append(rune.ToString());
        }

        return printable.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var font in _fonts.Values)
            font.Dispose();

        _fonts.Clear();
    }

    private static SKTypeface Typeface(PdfFontStyle style) => style == PdfFontStyle.Bold ? _bold.Value : _regular.Value;

    private static SKTypeface Load(string fileName)
    {
        // The typeface owns the stream and closes it.
        var stream = typeof(PdfFonts).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"The font {fileName} is not embedded in the assembly.");

        return SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException($"The font {fileName} could not be loaded.");
    }
}

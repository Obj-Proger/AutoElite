using AutoElite.Mobile.Theming;

namespace AutoElite.Mobile.Controls;

/// <summary>What a status, a notice or an icon tile tells: its color, the same in every view.</summary>
public enum StatusTone
{
    /// <summary>Plain: a grey tile and secondary text.</summary>
    Neutral,

    /// <summary>The gold of the school: soon, now, waiting for the user.</summary>
    Accent,

    /// <summary>Passed, paid, held.</summary>
    Success,

    /// <summary>Due soon, needs attention.</summary>
    Warning,

    /// <summary>Failed, overdue, cancelled.</summary>
    Danger
}

/// <summary>The colors of a <see cref="StatusTone"/> in the current theme.</summary>
internal static class ToneColors
{
    // How much of the color of a tone tints its background.
    private const float Tint = 0.16f;

    /// <summary>Gets the resource key of the color of text and icons of a tone.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>The resource key.</returns>
    public static string ForegroundOf(StatusTone tone) => tone switch
    {
        StatusTone.Accent => "AccentText",
        StatusTone.Success => "Success",
        StatusTone.Warning => "Warning",
        StatusTone.Danger => "Danger",
        _ => "TextSecondary"
    };

    /// <summary>Gets the resource key of the fill of a bar of a tone, such as a progress bar.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>The resource key.</returns>
    public static string FillOf(StatusTone tone) => tone switch
    {
        StatusTone.Neutral => "TextMuted",
        StatusTone.Accent => "Accent",
        _ => ForegroundOf(tone)
    };

    /// <summary>Gets the background of a tone: a tint of its color, or a raised surface for a plain one.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>The color.</returns>
    public static Color BackgroundOf(StatusTone tone) => tone == StatusTone.Neutral
        ? AppPalette.ColorOf("SurfaceRaised")
        : AppPalette.ColorOf(FillOf(tone)).WithAlpha(Tint);
}

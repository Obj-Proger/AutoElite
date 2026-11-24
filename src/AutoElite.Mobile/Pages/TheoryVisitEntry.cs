using AutoElite.Mobile.Controls;

namespace AutoElite.Mobile.Pages;

/// <summary>A past theory lesson of a student with whether they attended it, in their studies.</summary>
public sealed class TheoryVisitEntry
{
    /// <summary>Gets the topic of the lesson.</summary>
    public required string Topic { get; init; }

    /// <summary>Gets when the lesson was, with its instructor.</summary>
    public required string Details { get; init; }

    /// <summary>Gets whether the student attended: attended or missed.</summary>
    public required string State { get; init; }

    /// <summary>Gets the icon of the attendance: a check or a cross.</summary>
    public required string Icon { get; init; }

    /// <summary>Gets the tone of the attendance: success or danger.</summary>
    public StatusTone Tone { get; init; }
}

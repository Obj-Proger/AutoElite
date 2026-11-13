using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>A driving skill on the completion page, which the instructor grades from 1 to 5 or leaves ungraded.</summary>
public sealed class SkillGradeEntry : ViewModel
{
    /// <summary>Gets the choices of the grade: none, then 1 to 5, so that the index of a grade is the grade itself.</summary>
    public static IReadOnlyList<string> Choices { get; } = ["—", "1", "2", "3", "4", "5"];

    /// <summary>Gets the skill.</summary>
    public required DrivingSkill Skill { get; init; }

    /// <summary>Gets the name of the skill, in the language of the device.</summary>
    public required string Name { get; init; }

    /// <summary>Gets or sets the grade, or 0 when the skill is not graded.</summary>
    public int Grade
    {
        get;
        set => SetProperty(ref field, value);
    }
}

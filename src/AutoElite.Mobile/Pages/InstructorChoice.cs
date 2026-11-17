using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>An instructor whose lessons a manager chooses to see on the schedule, or all of them.</summary>
/// <param name="Id">The instructor, or <see langword="null"/> for all of them.</param>
/// <param name="Name">The full name of the instructor.</param>
public sealed record InstructorChoice(Guid? Id, string Name)
{
    /// <summary>Gets the choice of all instructors.</summary>
    public static InstructorChoice All => new(null, AppTexts.Get("Schedule.AllInstructors"));

    /// <inheritdoc />
    public override string ToString() => Name;
}

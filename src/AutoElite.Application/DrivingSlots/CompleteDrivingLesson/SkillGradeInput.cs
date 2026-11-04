namespace AutoElite.Application.DrivingSlots.CompleteDrivingLesson;

/// <summary>A grade of one driving skill.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Grade">The grade, from 1 to 5.</param>
public sealed record SkillGradeInput(DrivingSkill Skill, int Grade);

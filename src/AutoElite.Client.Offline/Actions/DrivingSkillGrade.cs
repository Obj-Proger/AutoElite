using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>The grade of one driving skill at a lesson.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Grade">The grade, from 1 to 5.</param>
public sealed record DrivingSkillGrade(DrivingSkill Skill, int Grade);

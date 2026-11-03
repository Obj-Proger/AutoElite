using System.Globalization;
using AutoElite.Domain.Enums;

namespace AutoElite.Domain.ValueObjects;

/// <summary>The grade an instructor gave for one driving skill, from 1 (poor) to 5 (excellent).</summary>
public sealed class SkillGrade : ValueObject
{
    /// <summary>The lowest grade.</summary>
    public const int MinGrade = 1;

    /// <summary>The highest grade.</summary>
    public const int MaxGrade = 5;

    /// <summary>Gets the graded skill.</summary>
    public DrivingSkill Skill { get; }

    /// <summary>Gets the grade.</summary>
    public int Grade { get; }

    private SkillGrade(DrivingSkill skill, int grade)
    {
        Skill = skill;
        Grade = grade;
    }

    /// <summary>Validates and creates a skill grade.</summary>
    public static Result<SkillGrade> Create(DrivingSkill skill, int grade)
    {
        var skillResult = Guard.DefinedEnum(skill, DomainErrors.SkillGrade.SkillNotDefined);
        if (skillResult.IsFailure)
            return Result.Failure<SkillGrade>(skillResult.Error);

        if (grade is < MinGrade or > MaxGrade)
            return Result.Failure<SkillGrade>(DomainErrors.SkillGrade.GradeOutOfRange);

        return new SkillGrade(skill, grade);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Skill;
        yield return Grade;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Skill}: {Grade}");
}

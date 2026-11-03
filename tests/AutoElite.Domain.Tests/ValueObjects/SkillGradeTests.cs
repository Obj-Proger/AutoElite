namespace AutoElite.Domain.Tests.ValueObjects;

public class SkillGradeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_GradeWithinRange_ReturnsGrade(int grade)
    {
        var result = SkillGrade.Create(DrivingSkill.Manoeuvring, grade);

        result.IsSuccess.Should().BeTrue();
        result.Value.Skill.Should().Be(DrivingSkill.Manoeuvring);
        result.Value.Grade.Should().Be(grade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_GradeOutOfRange_ReturnsGradeOutOfRangeError(int grade)
    {
        var result = SkillGrade.Create(DrivingSkill.Manoeuvring, grade);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.SkillGrade.GradeOutOfRange);
    }

    [Fact]
    public void Create_UndefinedSkill_ReturnsSkillNotDefinedError()
    {
        var result = SkillGrade.Create((DrivingSkill)99, 4);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.SkillGrade.SkillNotDefined);
    }

    [Fact]
    public void ToString_ReturnsSkillAndGrade()
    {
        SkillGrade.Create(DrivingSkill.CityDriving, 4).Value.ToString().Should().Be("CityDriving: 4");
    }
}

namespace AutoElite.Domain.Tests.ValueObjects;

public class TestRulesTests
{
    private static Result<TestRules> Create(
        int questionCount = 20, int timeLimitMinutes = 20, int maxMistakes = 2, int extraQuestions = 5, int extraTimeMinutes = 5) =>
        TestRules.Create(questionCount, TimeSpan.FromMinutes(timeLimitMinutes), maxMistakes, extraQuestions, TimeSpan.FromMinutes(extraTimeMinutes));

    [Fact]
    public void Create_ValidValues_ReturnsRules()
    {
        var result = Create(questionCount: 40, timeLimitMinutes: 30, maxMistakes: 4, extraQuestions: 10, extraTimeMinutes: 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuestionCount.Should().Be(40);
        result.Value.TimeLimit.Should().Be(TimeSpan.FromMinutes(30));
        result.Value.MaxMistakes.Should().Be(4);
        result.Value.ExtraQuestionsPerMistake.Should().Be(10);
        result.Value.ExtraTimePerMistake.Should().Be(TimeSpan.FromMinutes(10));
        result.Value.HasAddOnQuestions.Should().BeTrue();
    }

    [Fact]
    public void TrafficPoliceExam_MatchesTheOfficialExam()
    {
        var rules = TestRules.TrafficPoliceExam;

        rules.Should().Be(Create().Value);
    }

    [Fact]
    public void Create_NoAddOnQuestions_ReturnsRulesWithoutAddOns()
    {
        var result = Create(extraQuestions: 0, extraTimeMinutes: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasAddOnQuestions.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_QuestionCountOutOfRange_ReturnsQuestionCountOutOfRangeError(int questionCount)
    {
        var result = Create(questionCount: questionCount, maxMistakes: 0);

        result.Error.Should().Be(DomainErrors.TestRules.QuestionCountOutOfRange);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(181)]
    public void Create_TimeLimitOutOfRange_ReturnsTimeLimitOutOfRangeError(int minutes)
    {
        var result = Create(timeLimitMinutes: minutes);

        result.Error.Should().Be(DomainErrors.TestRules.TimeLimitOutOfRange);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(180)]
    public void Create_TimeLimitAtLimit_ReturnsRules(int minutes)
    {
        var result = Create(timeLimitMinutes: minutes);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(20)]
    public void Create_MaxMistakesOutOfRange_ReturnsMaxMistakesOutOfRangeError(int maxMistakes)
    {
        var result = Create(maxMistakes: maxMistakes);

        result.Error.Should().Be(DomainErrors.TestRules.MaxMistakesOutOfRange);
    }

    [Fact]
    public void Create_MaxMistakesOneBelowQuestionCount_ReturnsRules()
    {
        var result = Create(maxMistakes: 19);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Create_ExtraQuestionsOutOfRange_ReturnsExtraQuestionsOutOfRangeError(int extraQuestions)
    {
        var result = Create(extraQuestions: extraQuestions);

        result.Error.Should().Be(DomainErrors.TestRules.ExtraQuestionsOutOfRange);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void Create_ExtraTimeOutOfRange_ReturnsExtraTimeOutOfRangeError(int extraTimeMinutes)
    {
        var result = Create(extraTimeMinutes: extraTimeMinutes);

        result.Error.Should().Be(DomainErrors.TestRules.ExtraTimeOutOfRange);
    }

    [Fact]
    public void Create_ExtraTimeWithoutExtraQuestions_ReturnsExtraTimeWithoutExtraQuestionsError()
    {
        var result = Create(extraQuestions: 0, extraTimeMinutes: 5);

        result.Error.Should().Be(DomainErrors.TestRules.ExtraTimeWithoutExtraQuestions);
    }
}

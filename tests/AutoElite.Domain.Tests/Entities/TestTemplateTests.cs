namespace AutoElite.Domain.Tests.Entities;

public class TestTemplateTests
{
    private static readonly Guid[] _topicIds =
    [
        new("10000000-0000-0000-0000-000000000001"),
        new("10000000-0000-0000-0000-000000000002"),
        new("10000000-0000-0000-0000-000000000003"),
        new("10000000-0000-0000-0000-000000000004")
    ];

    private static readonly TestRules _topicTestRules =
        TestRules.Create(10, TimeSpan.FromMinutes(10), 1, null, 0, TimeSpan.Zero).Value;

    private static TestBlock Block(int questionCount, params Guid[] topicIds) => TestBlock.Create(questionCount, topicIds).Value;

    private static TestBlock[] ExamBlocks() => [.. _topicIds.Select(topicId => Block(5, topicId))];

    private static TestTemplate CreateExamTemplate() =>
        TestTemplate.Create("Exam ticket, category B", LicenseCategory.B, TestRules.TrafficPoliceExam, ExamBlocks(), QuestionSource.Official).Value;

    [Fact]
    public void Create_OfficialExamWithFourBlocks_ReturnsActiveTemplate()
    {
        var blocks = ExamBlocks();

        var result = TestTemplate.Create("  Exam ticket, category B  ", LicenseCategory.B, TestRules.TrafficPoliceExam, blocks, QuestionSource.Official);

        result.IsSuccess.Should().BeTrue();
        var template = result.Value;
        template.Name.Should().Be("Exam ticket, category B");
        template.Category.Should().Be(LicenseCategory.B);
        template.Rules.Should().Be(TestRules.TrafficPoliceExam);
        template.Blocks.Should().Equal(blocks);
        template.Source.Should().Be(QuestionSource.Official);
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_SingleBlockOfAllTopics_ReturnsTemplate()
    {
        var result = TestTemplate.Create("Mixed practice", LicenseCategory.B, _topicTestRules, [Block(10)], null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = TestTemplate.Create(name, LicenseCategory.B, _topicTestRules, [Block(10)], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.NameEmpty);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = TestTemplate.Create(new string('a', 201), LicenseCategory.B, _topicTestRules, [Block(10)], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.NameTooLong);
    }

    [Fact]
    public void Create_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = TestTemplate.Create("Ticket", (LicenseCategory)99, _topicTestRules, [Block(10)], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.CategoryNotDefined);
    }

    [Fact]
    public void Create_NoBlocks_ReturnsNoBlocksError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, _topicTestRules, [], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.NoBlocks);
    }

    [Fact]
    public void Create_ElevenBlocks_ReturnsTooManyBlocksError()
    {
        var rules = TestRules.Create(11, TimeSpan.FromMinutes(11), 1, null, 0, TimeSpan.Zero).Value;
        TestBlock[] blocks = [.. Enumerable.Range(1, 11).Select(index => Block(1, new Guid(index, 0, 0, new byte[8])))];

        var result = TestTemplate.Create("Ticket", LicenseCategory.B, rules, blocks, null);

        result.Error.Should().Be(DomainErrors.TestTemplate.TooManyBlocks);
    }

    [Fact]
    public void Create_AllTopicsBlockWithAnotherBlock_ReturnsAllTopicsBlockNotAloneError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, _topicTestRules, [Block(5), Block(5, _topicIds[0])], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.AllTopicsBlockNotAlone);
    }

    [Fact]
    public void Create_TopicInTwoBlocks_ReturnsTopicInSeveralBlocksError()
    {
        var result = TestTemplate.Create(
            "Ticket", LicenseCategory.B, _topicTestRules, [Block(5, _topicIds[0]), Block(5, _topicIds[0], _topicIds[1])], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.TopicInSeveralBlocks);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void Create_BlocksNotMatchingQuestionCount_ReturnsBlocksDoNotMatchQuestionCountError(int blockQuestions)
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, _topicTestRules, [Block(blockQuestions)], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.BlocksDoNotMatchQuestionCount);
    }

    [Fact]
    public void Create_UndefinedSource_ReturnsSourceNotDefinedError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, _topicTestRules, [Block(10)], (QuestionSource)9);

        result.Error.Should().Be(DomainErrors.TestTemplate.SourceNotDefined);
    }

    [Fact]
    public void UpdateDetails_ActiveTemplate_ReplacesSettings()
    {
        var template = CreateExamTemplate();
        TestBlock[] blocks = [Block(10, _topicIds[0])];

        var result = template.UpdateDetails("Road signs", LicenseCategory.A, _topicTestRules, blocks, QuestionSource.School);

        result.IsSuccess.Should().BeTrue();
        template.Name.Should().Be("Road signs");
        template.Category.Should().Be(LicenseCategory.A);
        template.Rules.Should().Be(_topicTestRules);
        template.Blocks.Should().Equal(blocks);
        template.Source.Should().Be(QuestionSource.School);
    }

    [Fact]
    public void UpdateDetails_BlocksNotMatchingNewRules_ReturnsErrorAndKeepsSettings()
    {
        var template = CreateExamTemplate();

        var result = template.UpdateDetails("Road signs", LicenseCategory.A, _topicTestRules, ExamBlocks(), null);

        result.Error.Should().Be(DomainErrors.TestTemplate.BlocksDoNotMatchQuestionCount);
        template.Name.Should().Be("Exam ticket, category B");
        template.Rules.Should().Be(TestRules.TrafficPoliceExam);
    }

    [Fact]
    public void UpdateDetails_InactiveTemplate_ReturnsCannotModifyInactiveError()
    {
        var template = CreateExamTemplate();
        template.Deactivate();

        var result = template.UpdateDetails("Road signs", LicenseCategory.B, _topicTestRules, [Block(10)], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var template = CreateExamTemplate();
        template.Deactivate();

        var result = template.Reactivate();

        result.IsSuccess.Should().BeTrue();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveTemplate_ReturnsAlreadyInactiveError()
    {
        var template = CreateExamTemplate();
        template.Deactivate();

        var result = template.Deactivate();

        result.Error.Should().Be(DomainErrors.TestTemplate.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveTemplate_ReturnsAlreadyActiveError()
    {
        var template = CreateExamTemplate();

        var result = template.Reactivate();

        result.Error.Should().Be(DomainErrors.TestTemplate.AlreadyActive);
    }
}

namespace AutoElite.Domain.Tests.Entities;

public class TestTemplateTests
{
    private static readonly Guid _signsTopicId = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid _markingsTopicId = new("88888888-8888-8888-8888-888888888888");
    private static readonly TestRules _topicTestRules =
        TestRules.Create(10, TimeSpan.FromMinutes(10), 1, 0, TimeSpan.Zero).Value;

    private static TestTemplate CreateTemplate() =>
        TestTemplate.Create("Exam ticket, category B", LicenseCategory.B, TestRules.TrafficPoliceExam, [], QuestionSource.Official).Value;

    [Fact]
    public void Create_ValidData_ReturnsActiveTemplateWithGivenValues()
    {
        var result = TestTemplate.Create(
            "  Road signs  ", LicenseCategory.B, _topicTestRules, [_signsTopicId, _markingsTopicId], null);

        result.IsSuccess.Should().BeTrue();
        var template = result.Value;
        template.Name.Should().Be("Road signs");
        template.Category.Should().Be(LicenseCategory.B);
        template.Rules.Should().Be(_topicTestRules);
        template.TopicIds.Should().Equal(_signsTopicId, _markingsTopicId);
        template.Source.Should().BeNull();
        template.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = TestTemplate.Create(name, LicenseCategory.B, TestRules.TrafficPoliceExam, [], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.NameEmpty);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = TestTemplate.Create(new string('a', 201), LicenseCategory.B, TestRules.TrafficPoliceExam, [], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.NameTooLong);
    }

    [Fact]
    public void Create_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = TestTemplate.Create("Ticket", (LicenseCategory)99, TestRules.TrafficPoliceExam, [], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.CategoryNotDefined);
    }

    [Fact]
    public void Create_EmptyTopicId_ReturnsTopicIdEmptyError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, TestRules.TrafficPoliceExam, [Guid.Empty], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.TopicIdEmpty);
    }

    [Fact]
    public void Create_RepeatedTopic_ReturnsDuplicateTopicError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, TestRules.TrafficPoliceExam, [_signsTopicId, _signsTopicId], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.DuplicateTopic);
    }

    [Fact]
    public void Create_UndefinedSource_ReturnsSourceNotDefinedError()
    {
        var result = TestTemplate.Create("Ticket", LicenseCategory.B, TestRules.TrafficPoliceExam, [], (QuestionSource)9);

        result.Error.Should().Be(DomainErrors.TestTemplate.SourceNotDefined);
    }

    [Fact]
    public void UpdateDetails_ActiveTemplate_ReplacesSettings()
    {
        var template = CreateTemplate();

        var result = template.UpdateDetails("Road signs", LicenseCategory.A, _topicTestRules, [_signsTopicId], QuestionSource.School);

        result.IsSuccess.Should().BeTrue();
        template.Name.Should().Be("Road signs");
        template.Category.Should().Be(LicenseCategory.A);
        template.Rules.Should().Be(_topicTestRules);
        template.TopicIds.Should().Equal(_signsTopicId);
        template.Source.Should().Be(QuestionSource.School);
    }

    [Fact]
    public void UpdateDetails_InvalidTopics_ReturnsErrorAndKeepsSettings()
    {
        var template = CreateTemplate();

        var result = template.UpdateDetails("Road signs", LicenseCategory.A, _topicTestRules, [Guid.Empty], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.TopicIdEmpty);
        template.Name.Should().Be("Exam ticket, category B");
        template.Rules.Should().Be(TestRules.TrafficPoliceExam);
    }

    [Fact]
    public void UpdateDetails_InactiveTemplate_ReturnsCannotModifyInactiveError()
    {
        var template = CreateTemplate();
        template.Deactivate();

        var result = template.UpdateDetails("Road signs", LicenseCategory.B, _topicTestRules, [], null);

        result.Error.Should().Be(DomainErrors.TestTemplate.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var template = CreateTemplate();
        template.Deactivate();

        var result = template.Reactivate();

        result.IsSuccess.Should().BeTrue();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveTemplate_ReturnsAlreadyInactiveError()
    {
        var template = CreateTemplate();
        template.Deactivate();

        var result = template.Deactivate();

        result.Error.Should().Be(DomainErrors.TestTemplate.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveTemplate_ReturnsAlreadyActiveError()
    {
        var template = CreateTemplate();

        var result = template.Reactivate();

        result.Error.Should().Be(DomainErrors.TestTemplate.AlreadyActive);
    }
}

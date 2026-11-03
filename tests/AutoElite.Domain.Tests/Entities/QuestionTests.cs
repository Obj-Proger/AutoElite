namespace AutoElite.Domain.Tests.Entities;

public class QuestionTests
{
    private static readonly Guid _topicId = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid _otherTopicId = new("88888888-8888-8888-8888-888888888888");

    private static readonly AnswerOptionDraft[] _options =
    [
        new("Only the car", false),
        new("Only the tram", true),
        new("Both", false)
    ];

    private static readonly LicenseCategory[] _abm = [LicenseCategory.B, LicenseCategory.A, LicenseCategory.M];

    private static Question CreateQuestion() =>
        Question.Create("Who has the right of way?", null, null, _topicId, _abm, QuestionSource.Official, "ABM-1-1", _options).Value;

    private static Result<Question> CreateWithOptions(params AnswerOptionDraft[] options) =>
        Question.Create("Who has the right of way?", null, null, _topicId, _abm, QuestionSource.School, null, options);

    private static Result<Question> CreateWithCategories(params LicenseCategory[] categories) =>
        Question.Create("Who has the right of way?", null, null, _topicId, categories, QuestionSource.School, null, _options);

    [Fact]
    public void Create_ValidData_ReturnsActiveQuestionWithGivenValues()
    {
        var result = Question.Create(
            "  Who has the right of way?  ", "  The tram, see 13.11  ", "  questions/abm-1-1.png  ",
            _topicId, _abm, QuestionSource.Official, "  ABM-1-1  ", _options);

        result.IsSuccess.Should().BeTrue();
        var question = result.Value;
        question.Text.Should().Be("Who has the right of way?");
        question.Explanation.Should().Be("The tram, see 13.11");
        question.ImageKey.Should().Be("questions/abm-1-1.png");
        question.TopicId.Should().Be(_topicId);
        question.Source.Should().Be(QuestionSource.Official);
        question.ExternalCode.Should().Be("ABM-1-1");
        question.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidData_StoresCategoriesInAscendingOrder()
    {
        var question = CreateQuestion();

        question.Categories.Should().Equal(LicenseCategory.A, LicenseCategory.B, LicenseCategory.M);
    }

    [Fact]
    public void Create_ValidData_NumbersOptionsAndExposesTheCorrectOne()
    {
        var question = CreateQuestion();

        question.Options.Select(option => option.Text).Should().Equal("Only the car", "Only the tram", "Both");
        question.Options.Select(option => option.Position).Should().Equal(1, 2, 3);
        question.CorrectOption.Text.Should().Be("Only the tram");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyText_ReturnsTextEmptyError(string? text)
    {
        var result = Question.Create(text, null, null, _topicId, _abm, QuestionSource.School, null, _options);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Question.TextEmpty);
    }

    [Fact]
    public void Create_TextTooLong_ReturnsTextTooLongError()
    {
        var result = Question.Create(new string('a', 2001), null, null, _topicId, _abm, QuestionSource.School, null, _options);

        result.Error.Should().Be(DomainErrors.Question.TextTooLong);
    }

    [Fact]
    public void Create_ExplanationTooLong_ReturnsExplanationTooLongError()
    {
        var result = Question.Create("Text", new string('a', 4001), null, _topicId, _abm, QuestionSource.School, null, _options);

        result.Error.Should().Be(DomainErrors.Question.ExplanationTooLong);
    }

    [Fact]
    public void Create_ImageKeyTooLong_ReturnsImageKeyTooLongError()
    {
        var result = Question.Create("Text", null, new string('a', 2049), _topicId, _abm, QuestionSource.School, null, _options);

        result.Error.Should().Be(DomainErrors.Question.ImageKeyTooLong);
    }

    [Fact]
    public void Create_EmptyTopicId_ReturnsTopicIdEmptyError()
    {
        var result = Question.Create("Text", null, null, Guid.Empty, _abm, QuestionSource.School, null, _options);

        result.Error.Should().Be(DomainErrors.Question.TopicIdEmpty);
    }

    [Fact]
    public void Create_NoCategories_ReturnsNoCategoriesError()
    {
        var result = CreateWithCategories();

        result.Error.Should().Be(DomainErrors.Question.NoCategories);
    }

    [Fact]
    public void Create_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = CreateWithCategories(LicenseCategory.B, (LicenseCategory)99);

        result.Error.Should().Be(DomainErrors.Question.CategoryNotDefined);
    }

    [Fact]
    public void Create_RepeatedCategory_ReturnsDuplicateCategoryError()
    {
        var result = CreateWithCategories(LicenseCategory.B, LicenseCategory.B);

        result.Error.Should().Be(DomainErrors.Question.DuplicateCategory);
    }

    [Fact]
    public void Create_UndefinedSource_ReturnsSourceNotDefinedError()
    {
        var result = Question.Create("Text", null, null, _topicId, _abm, (QuestionSource)0, null, _options);

        result.Error.Should().Be(DomainErrors.Question.SourceNotDefined);
    }

    [Fact]
    public void Create_ExternalCodeTooLong_ReturnsExternalCodeTooLongError()
    {
        var result = Question.Create("Text", null, null, _topicId, _abm, QuestionSource.Official, new string('a', 51), _options);

        result.Error.Should().Be(DomainErrors.Question.ExternalCodeTooLong);
    }

    [Fact]
    public void Create_OneOption_ReturnsTooFewOptionsError()
    {
        var result = CreateWithOptions(new AnswerOptionDraft("Yes", true));

        result.Error.Should().Be(DomainErrors.Question.TooFewOptions);
    }

    [Fact]
    public void Create_SixOptions_ReturnsQuestion()
    {
        var result = CreateWithOptions(
            new("1", true), new("2", false), new("3", false), new("4", false), new("5", false), new("6", false));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_SevenOptions_ReturnsTooManyOptionsError()
    {
        var result = CreateWithOptions(
            new("1", true), new("2", false), new("3", false), new("4", false), new("5", false), new("6", false), new("7", false));

        result.Error.Should().Be(DomainErrors.Question.TooManyOptions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyOptionText_ReturnsOptionTextEmptyError(string? optionText)
    {
        var result = CreateWithOptions(new("Yes", true), new(optionText, false));

        result.Error.Should().Be(DomainErrors.Question.OptionTextEmpty);
    }

    [Fact]
    public void Create_OptionTextTooLong_ReturnsOptionTextTooLongError()
    {
        var result = CreateWithOptions(new("Yes", true), new(new string('a', 1001), false));

        result.Error.Should().Be(DomainErrors.Question.OptionTextTooLong);
    }

    [Fact]
    public void Create_OptionsWithSameTextInOtherCase_ReturnsDuplicateOptionTextError()
    {
        var result = CreateWithOptions(new("Yes", true), new(" YES ", false));

        result.Error.Should().Be(DomainErrors.Question.DuplicateOptionText);
    }

    [Fact]
    public void Create_NoCorrectOption_ReturnsNotExactlyOneCorrectOptionError()
    {
        var result = CreateWithOptions(new("Yes", false), new("No", false));

        result.Error.Should().Be(DomainErrors.Question.NotExactlyOneCorrectOption);
    }

    [Fact]
    public void Create_TwoCorrectOptions_ReturnsNotExactlyOneCorrectOptionError()
    {
        var result = CreateWithOptions(new("Yes", true), new("No", true));

        result.Error.Should().Be(DomainErrors.Question.NotExactlyOneCorrectOption);
    }

    [Fact]
    public void UpdateContent_ActiveQuestion_ReplacesContentAndOptionsAndKeepsSource()
    {
        var question = CreateQuestion();
        AnswerOptionDraft[] newOptions = [new("Yes", false), new("No", true)];

        var result = question.UpdateContent("Can you overtake here?", "Sign 3.20", null, _otherTopicId, [LicenseCategory.C], null, newOptions);

        result.IsSuccess.Should().BeTrue();
        question.Text.Should().Be("Can you overtake here?");
        question.Explanation.Should().Be("Sign 3.20");
        question.TopicId.Should().Be(_otherTopicId);
        question.Categories.Should().Equal(LicenseCategory.C);
        question.ExternalCode.Should().BeNull();
        question.Options.Select(option => option.Text).Should().Equal("Yes", "No");
        question.CorrectOption.Text.Should().Be("No");
        question.Source.Should().Be(QuestionSource.Official);
    }

    [Fact]
    public void UpdateContent_InvalidOptions_ReturnsErrorAndKeepsContent()
    {
        var question = CreateQuestion();

        var result = question.UpdateContent("Can you overtake here?", null, null, _topicId, _abm, null, [new("Yes", true)]);

        result.Error.Should().Be(DomainErrors.Question.TooFewOptions);
        question.Text.Should().Be("Who has the right of way?");
        question.Options.Should().HaveCount(3);
    }

    [Fact]
    public void UpdateContent_InactiveQuestion_ReturnsCannotModifyInactiveError()
    {
        var question = CreateQuestion();
        question.Deactivate();

        var result = question.UpdateContent("Can you overtake here?", null, null, _topicId, _abm, null, _options);

        result.Error.Should().Be(DomainErrors.Question.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var question = CreateQuestion();
        question.Deactivate();

        var result = question.Reactivate();

        result.IsSuccess.Should().BeTrue();
        question.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveQuestion_ReturnsAlreadyInactiveError()
    {
        var question = CreateQuestion();
        question.Deactivate();

        var result = question.Deactivate();

        result.Error.Should().Be(DomainErrors.Question.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveQuestion_ReturnsAlreadyActiveError()
    {
        var question = CreateQuestion();

        var result = question.Reactivate();

        result.Error.Should().Be(DomainErrors.Question.AlreadyActive);
    }
}

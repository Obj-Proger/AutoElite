namespace AutoElite.Domain.Tests.Entities;

public class CourseTests
{
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Money _price = Money.Create(45000m, _rub).Value;
    private static readonly Money _otherPrice = Money.Create(52000m, _rub).Value;

    private static Course CreateCourse() =>
        Course.Create("Category B, standard", LicenseCategory.B, _price, 134, 56, null).Value;

    private static Course CreateInactiveCourse()
    {
        var course = CreateCourse();
        course.Deactivate();

        return course;
    }

    private static Course CreateCourseWithTopics(params string[] titles)
    {
        var course = CreateCourse();
        foreach (var title in titles)
            course.AddTopic(title, null);

        return course;
    }

    private static string[] TopicTitles(Course course) => [.. course.Topics.Select(topic => topic.Title)];

    private static int[] TopicPositions(Course course) => [.. course.Topics.Select(topic => topic.Position)];

    [Fact]
    public void Create_ValidData_ReturnsActiveCourseWithGivenValues()
    {
        var result = Course.Create("  Category B, standard  ", LicenseCategory.B, _price, 134, 56, "  Day and evening groups  ");

        result.IsSuccess.Should().BeTrue();
        var course = result.Value;
        course.Name.Should().Be("Category B, standard");
        course.Category.Should().Be(LicenseCategory.B);
        course.Price.Should().Be(_price);
        course.TheoryHours.Should().Be(134);
        course.PracticeHours.Should().Be(56);
        course.Description.Should().Be("Day and evening groups");
        course.IsActive.Should().BeTrue();
        course.Topics.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = Course.Create(name, LicenseCategory.B, _price, 134, 56, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.NameEmpty);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = Course.Create(new string('a', 201), LicenseCategory.B, _price, 134, 56, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.NameTooLong);
    }

    [Fact]
    public void Create_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = Course.Create("Course", (LicenseCategory)99, _price, 134, 56, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.CategoryNotDefined);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_PriceNotPositive_ReturnsPriceNotPositiveError(int amount)
    {
        var price = Money.Create(amount, _rub).Value;

        var result = Course.Create("Course", LicenseCategory.B, price, 134, 56, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.PriceNotPositive);
    }

    [Theory]
    [InlineData(0, 56)]
    [InlineData(134, 0)]
    [InlineData(1000, 1000)]
    public void Create_HoursWithinRange_ReturnsCourse(int theoryHours, int practiceHours)
    {
        var result = Course.Create("Course", LicenseCategory.B, _price, theoryHours, practiceHours, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1, 56)]
    [InlineData(134, -1)]
    [InlineData(1001, 56)]
    [InlineData(134, 1001)]
    public void Create_HoursOutOfRange_ReturnsHoursOutOfRangeError(int theoryHours, int practiceHours)
    {
        var result = Course.Create("Course", LicenseCategory.B, _price, theoryHours, practiceHours, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.HoursOutOfRange);
    }

    [Fact]
    public void Create_NoHours_ReturnsNoHoursError()
    {
        var result = Course.Create("Course", LicenseCategory.B, _price, 0, 0, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.NoHours);
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsDescriptionTooLongError()
    {
        var result = Course.Create("Course", LicenseCategory.B, _price, 134, 56, new string('a', 2001));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.DescriptionTooLong);
    }

    [Fact]
    public void UpdateDetails_ActiveCourse_ReplacesDetails()
    {
        var course = CreateCourse();

        var result = course.UpdateDetails("Category A", LicenseCategory.A, _otherPrice, 130, 18, "Motorcycles");

        result.IsSuccess.Should().BeTrue();
        course.Name.Should().Be("Category A");
        course.Category.Should().Be(LicenseCategory.A);
        course.Price.Should().Be(_otherPrice);
        course.TheoryHours.Should().Be(130);
        course.PracticeHours.Should().Be(18);
        course.Description.Should().Be("Motorcycles");
    }

    [Fact]
    public void UpdateDetails_InvalidValue_ReturnsErrorAndKeepsDetails()
    {
        var course = CreateCourse();

        var result = course.UpdateDetails("Category A", LicenseCategory.A, _otherPrice, 0, 0, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.NoHours);
        course.Name.Should().Be("Category B, standard");
        course.Price.Should().Be(_price);
    }

    [Fact]
    public void UpdateDetails_InactiveCourse_ReturnsCannotModifyInactiveError()
    {
        var course = CreateInactiveCourse();

        var result = course.UpdateDetails("Category A", LicenseCategory.A, _otherPrice, 130, 18, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ActiveCourse_BecomesInactive()
    {
        var course = CreateCourse();

        var result = course.Deactivate();

        result.IsSuccess.Should().BeTrue();
        course.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_InactiveCourse_ReturnsAlreadyInactiveError()
    {
        var course = CreateInactiveCourse();

        var result = course.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_InactiveCourse_BecomesActive()
    {
        var course = CreateInactiveCourse();

        var result = course.Reactivate();

        result.IsSuccess.Should().BeTrue();
        course.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_ActiveCourse_ReturnsAlreadyActiveError()
    {
        var course = CreateCourse();

        var result = course.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.AlreadyActive);
    }

    [Fact]
    public void AddTopic_ValidValues_AppendsTopicAtNextPosition()
    {
        var course = CreateCourseWithTopics("Road signs");

        var result = course.AddTopic("  Road markings  ", "  Lines and arrows  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Road markings");
        result.Value.Description.Should().Be("Lines and arrows");
        result.Value.Position.Should().Be(2);
        course.Topics.Should().HaveCount(2).And.EndWith(result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTopic_EmptyTitle_ReturnsTitleEmptyError(string? title)
    {
        var course = CreateCourse();

        var result = course.AddTopic(title, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CourseTopic.TitleEmpty);
        course.Topics.Should().BeEmpty();
    }

    [Fact]
    public void AddTopic_TitleTooLong_ReturnsTitleTooLongError()
    {
        var course = CreateCourse();

        var result = course.AddTopic(new string('a', 201), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CourseTopic.TitleTooLong);
    }

    [Fact]
    public void AddTopic_DescriptionTooLong_ReturnsDescriptionTooLongError()
    {
        var course = CreateCourse();

        var result = course.AddTopic("Road signs", new string('a', 2001));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.CourseTopic.DescriptionTooLong);
    }

    [Fact]
    public void AddTopic_TitleOfExistingTopicInOtherCase_ReturnsDuplicateTopicTitleError()
    {
        var course = CreateCourseWithTopics("Road signs");

        var result = course.AddTopic("ROAD SIGNS", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.DuplicateTopicTitle);
        course.Topics.Should().ContainSingle();
    }

    [Fact]
    public void AddTopic_InactiveCourse_ReturnsCannotModifyInactiveError()
    {
        var course = CreateInactiveCourse();

        var result = course.AddTopic("Road signs", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.CannotModifyInactive);
    }

    [Fact]
    public void UpdateTopic_ValidValues_ChangesTitleAndDescription()
    {
        var course = CreateCourseWithTopics("Road signs");
        var topic = course.Topics[0];

        var result = course.UpdateTopic(topic.Id, "Road signs and markings", "Chapter 1");

        result.IsSuccess.Should().BeTrue();
        topic.Title.Should().Be("Road signs and markings");
        topic.Description.Should().Be("Chapter 1");
    }

    [Fact]
    public void UpdateTopic_SameTitleInOtherCase_ChangesTitle()
    {
        var course = CreateCourseWithTopics("Road signs");
        var topic = course.Topics[0];

        var result = course.UpdateTopic(topic.Id, "Road Signs", null);

        result.IsSuccess.Should().BeTrue();
        topic.Title.Should().Be("Road Signs");
    }

    [Fact]
    public void UpdateTopic_TitleOfAnotherTopic_ReturnsDuplicateTopicTitleError()
    {
        var course = CreateCourseWithTopics("Road signs", "Road markings");

        var result = course.UpdateTopic(course.Topics[1].Id, "road signs", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.DuplicateTopicTitle);
        course.Topics[1].Title.Should().Be("Road markings");
    }

    [Fact]
    public void UpdateTopic_UnknownTopic_ReturnsTopicNotFoundError()
    {
        var course = CreateCourseWithTopics("Road signs");

        var result = course.UpdateTopic(Guid.CreateVersion7(), "Road markings", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.TopicNotFound);
    }

    [Fact]
    public void UpdateTopic_InactiveCourse_ReturnsCannotModifyInactiveError()
    {
        var course = CreateCourseWithTopics("Road signs");
        var topicId = course.Topics[0].Id;
        course.Deactivate();

        var result = course.UpdateTopic(topicId, "Road markings", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.CannotModifyInactive);
    }

    [Theory]
    [InlineData(0, 3, new[] { "B", "C", "A" })]
    [InlineData(2, 1, new[] { "C", "A", "B" })]
    [InlineData(1, 2, new[] { "A", "B", "C" })]
    [InlineData(1, 1, new[] { "B", "A", "C" })]
    public void MoveTopic_ValidPosition_ReordersAndKeepsPositionsContiguous(int topicIndex, int newPosition, string[] expectedOrder)
    {
        var course = CreateCourseWithTopics("A", "B", "C");

        var result = course.MoveTopic(course.Topics[topicIndex].Id, newPosition);

        result.IsSuccess.Should().BeTrue();
        TopicTitles(course).Should().Equal(expectedOrder);
        TopicPositions(course).Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void MoveTopic_PositionOutOfRange_ReturnsTopicPositionOutOfRangeError(int newPosition)
    {
        var course = CreateCourseWithTopics("A", "B", "C");

        var result = course.MoveTopic(course.Topics[0].Id, newPosition);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.TopicPositionOutOfRange);
        TopicTitles(course).Should().Equal("A", "B", "C");
    }

    [Fact]
    public void MoveTopic_UnknownTopic_ReturnsTopicNotFoundError()
    {
        var course = CreateCourseWithTopics("A", "B");

        var result = course.MoveTopic(Guid.CreateVersion7(), 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.TopicNotFound);
    }

    [Fact]
    public void RemoveTopic_ExistingTopic_RemovesItAndClosesTheGap()
    {
        var course = CreateCourseWithTopics("A", "B", "C");

        var result = course.RemoveTopic(course.Topics[0].Id);

        result.IsSuccess.Should().BeTrue();
        TopicTitles(course).Should().Equal("B", "C");
        TopicPositions(course).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveTopic_UnknownTopic_ReturnsTopicNotFoundError()
    {
        var course = CreateCourseWithTopics("A");

        var result = course.RemoveTopic(Guid.CreateVersion7());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.TopicNotFound);
        course.Topics.Should().ContainSingle();
    }

    [Fact]
    public void RemoveTopic_InactiveCourse_ReturnsCannotModifyInactiveError()
    {
        var course = CreateCourseWithTopics("A");
        var topicId = course.Topics[0].Id;
        course.Deactivate();

        var result = course.RemoveTopic(topicId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Course.CannotModifyInactive);
    }
}

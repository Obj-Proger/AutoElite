using AutoElite.Application.Courses.AddCourseTopic;
using AutoElite.Application.Courses.MoveCourseTopic;
using AutoElite.Application.Courses.RemoveCourseTopic;
using AutoElite.Application.Courses.UpdateCourseTopic;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Courses;

public class CourseTopicCommandHandlerTests
{
    private readonly FakeCourseRepository _courses = new();
    private readonly Course _course = TestData.NewCourse();

    public CourseTopicCommandHandlerTests()
    {
        _courses.Seed(_course);
    }

    private Task<Result<Guid>> Add(string title, Guid? courseId = null) =>
        new AddCourseTopicCommandHandler(_courses).Handle(
            new AddCourseTopicCommand(courseId ?? _course.Id, title, null), TestContext.Current.CancellationToken);

    private Task<Result> Remove(Guid topicId) =>
        new RemoveCourseTopicCommandHandler(_courses).Handle(
            new RemoveCourseTopicCommand(_course.Id, topicId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Add_NewTitle_AppendsTheTopic()
    {
        await Add("Road signs");

        var result = await Add("Traffic lights");

        var topic = _course.Topics.Should().HaveCount(2).And.Subject.Last();
        topic.Id.Should().Be(result.Value);
        topic.Position.Should().Be(2);
    }

    [Fact]
    public async Task Add_DuplicateTitle_ReturnsTheDomainError()
    {
        await Add("Road signs");

        var result = await Add("ROAD SIGNS");

        result.Error.Should().Be(DomainErrors.Course.DuplicateTopicTitle);
    }

    [Fact]
    public async Task Add_UnknownCourse_ReturnsCourseNotFound()
    {
        var result = await Add("Road signs", Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<Course>());
    }

    [Fact]
    public async Task Update_Topic_ChangesItsTitle()
    {
        var topicId = (await Add("Road signs")).Value;

        var result = await new UpdateCourseTopicCommandHandler(_courses).Handle(
            new UpdateCourseTopicCommand(_course.Id, topicId, "Signs and markings", "Chapter 1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _course.Topics.Single().Title.Should().Be("Signs and markings");
    }

    [Fact]
    public async Task Move_Topic_ReordersTheCurriculum()
    {
        await Add("Road signs");
        var lightsId = (await Add("Traffic lights")).Value;

        var result = await new MoveCourseTopicCommandHandler(_courses).Handle(
            new MoveCourseTopicCommand(_course.Id, lightsId, 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _course.Topics.OrderBy(topic => topic.Position).Select(topic => topic.Title).Should().Equal("Traffic lights", "Road signs");
    }

    [Fact]
    public async Task Remove_UnusedTopic_RemovesIt()
    {
        var topicId = (await Add("Road signs")).Value;

        var result = await Remove(topicId);

        result.IsSuccess.Should().BeTrue();
        _course.Topics.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_TopicInUse_ReturnsTopicInUseAndKeepsIt()
    {
        var topicId = (await Add("Road signs")).Value;
        _courses.TopicsInUse.Add(topicId);

        var result = await Remove(topicId);

        result.Error.Should().Be(ApplicationErrors.Course.TopicInUse);
        _course.Topics.Should().ContainSingle();
    }

    [Fact]
    public async Task Remove_TopicOfAnotherCourse_ReturnsTopicNotFound()
    {
        var foreignTopicId = Guid.CreateVersion7();
        _courses.TopicsInUse.Add(foreignTopicId);

        var result = await Remove(foreignTopicId);

        result.Error.Should().Be(DomainErrors.Course.TopicNotFound);
    }
}

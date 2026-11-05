using AutoElite.Application.Courses;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class CoursePersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly Currency _rubles = Currency.Create("RUB").Value;

    private readonly string _marker = Guid.NewGuid().ToString("N")[..12];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Course NewCourse(string name = "Категория B", LicenseCategory category = LicenseCategory.B, decimal price = 45_000.50m) =>
        Course.Create($"{name} {_marker}", category, Money.Create(price, _rubles).Value, 134, 56, "Вечерняя группа").Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task ChangeAsync(Guid courseId, Action<Course> change)
    {
        await using var context = database.CreateContext();
        var course = await new CourseRepository(context).GetByIdAsync(courseId, CancellationToken);
        change(course!);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task<Course> LoadAsync(Guid courseId)
    {
        await using var context = database.CreateContext();

        return (await new CourseRepository(context).GetByIdAsync(courseId, CancellationToken))!;
    }

    private static Guid AddTopic(Course course, string title) => course.AddTopic(title, null).Value.Id;

    [Fact]
    public async Task Save_NewCourse_StoresThePriceAndTopicsInOrder()
    {
        var course = NewCourse();
        AddTopic(course, "Общие положения");
        AddTopic(course, "Дорожные знаки");
        await SaveAsync(course);

        var loaded = await LoadAsync(course.Id);

        loaded.Price.Should().Be(Money.Create(45_000.50m, _rubles).Value);
        loaded.TheoryHours.Should().Be(134);
        loaded.PracticeHours.Should().Be(56);
        loaded.Description.Should().Be("Вечерняя группа");
        loaded.Topics.Select(topic => topic.Title).Should().Equal("Общие положения", "Дорожные знаки");
        loaded.Topics.Select(topic => topic.Position).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Save_MovedAndRemovedTopics_KeepsTheNewCurriculum()
    {
        var course = NewCourse();
        var first = AddTopic(course, "Первая");
        AddTopic(course, "Вторая");
        var third = AddTopic(course, "Третья");
        await SaveAsync(course);

        await ChangeAsync(course.Id, tracked =>
        {
            tracked.MoveTopic(third, 1).IsSuccess.Should().BeTrue();
            tracked.RemoveTopic(first).IsSuccess.Should().BeTrue();
        });

        var loaded = await LoadAsync(course.Id);
        loaded.Topics.Select(topic => topic.Title).Should().Equal("Третья", "Вторая");
        loaded.Topics.Select(topic => topic.Position).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetByTopic_FindsTheCourseOfTheTopic()
    {
        var course = NewCourse();
        var topicId = AddTopic(course, "Разметка");
        await SaveAsync(course, NewCourse("Другой курс"));
        await using var context = database.CreateContext();
        var courses = new CourseRepository(context);

        (await courses.GetByTopicAsync(topicId, CancellationToken))!.Id.Should().Be(course.Id);
        (await courses.GetByTopicAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetList_SortsByCategoryOrderThenNameAndCountsTopics()
    {
        var withTopics = NewCourse("Б курс", LicenseCategory.CE);
        AddTopic(withTopics, "Тема");
        var sameCategory = NewCourse("А курс", LicenseCategory.CE);
        var laterCategory = NewCourse("А курс", LicenseCategory.C1E);
        var inactive = NewCourse("В курс", LicenseCategory.CE);
        inactive.Deactivate().IsSuccess.Should().BeTrue();
        await SaveAsync(withTopics, sameCategory, laterCategory, inactive);
        await using var context = database.CreateContext();
        var queries = new CourseQueries(context);

        var all = (await queries.GetListAsync(null, null, CancellationToken))
            .Where(item => item.Name.EndsWith(_marker, StringComparison.Ordinal))
            .ToList();
        var activeCe = await queries.GetListAsync(LicenseCategory.CE, true, CancellationToken);

        all.Select(item => item.Id).Should().Equal(sameCategory.Id, withTopics.Id, inactive.Id, laterCategory.Id);
        all.Single(item => item.Id == withTopics.Id).TopicCount.Should().Be(1);
        all[0].PriceAmount.Should().Be(45_000.50m);
        all[0].PriceCurrency.Should().Be("RUB");
        activeCe.Should().OnlyContain(item => item.Category == LicenseCategory.CE && item.IsActive)
            .And.Contain(item => item.Id == withTopics.Id);
    }

    [Fact]
    public async Task Get_ReadsTheCourseWithItsCurriculum()
    {
        var course = NewCourse();
        var topicId = course.AddTopic("Сигналы светофора", "Регулировщик").Value.Id;
        await SaveAsync(course);
        await using var context = database.CreateContext();
        var queries = new CourseQueries(context);

        var details = await queries.GetAsync(course.Id, CancellationToken);

        details!.Name.Should().Be(course.Name);
        details.PriceCurrency.Should().Be("RUB");
        details.Topics.Should().Equal(new CourseTopicItem(topicId, 1, "Сигналы светофора", "Регулировщик"));
        (await queries.GetAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}

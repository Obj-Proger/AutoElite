using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TheoryLessons;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class TheoryLessonPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _lessonDay = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateTime Now => FixedClock.Default.UtcNow;

    private static TimeInterval Hours(int fromHour, int toHour, int day = 0) =>
        TimeInterval.Create(_lessonDay.AddDays(day).AddHours(fromHour), _lessonDay.AddDays(day).AddHours(toHour)).Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    /// <summary>A group of a course with one topic, its instructor, and one enrolled student.</summary>
    private async Task<Scene> SceneAsync()
    {
        var instructor = TrainingRecords.Person("Преподавателев");
        var student = TrainingRecords.Person("Слушателев");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var group = StudyGroup.Create(
                $"Группа {Guid.NewGuid():N}", course, instructor.Id, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 7, 1), 20)
            .Value;
        group.Enroll(contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(instructor, student, course, contract, group);

        return new Scene(instructor, student, course, topicId, contract, group);
    }

    private static TheoryLesson Lesson(Scene scene, TimeInterval time, Guid? instructorId = null) =>
        TheoryLesson.Schedule(scene.Group, scene.Course, scene.TopicId, instructorId ?? scene.Instructor.Id, time, Now).Value;

    [Fact]
    public async Task Save_Attendance_IsStoredWithTheLessonAndCountedInTheContract()
    {
        var scene = await SceneAsync();
        var lesson = Lesson(scene, Hours(9, 11));
        await SaveAsync(lesson);

        await using (var context = database.CreateContext())
        {
            var tracked = await new TheoryLessonRepository(context).GetByIdAsync(lesson.Id, CancellationToken);
            var group = await new StudyGroupRepository(context).GetByIdAsync(scene.Group.Id, CancellationToken);
            var contract = await new ContractRepository(context).GetByIdAsync(scene.Contract.Id, CancellationToken);
            tracked!.MarkAttendance(group!, contract!, true, "Без опоздания", scene.Instructor.Id, _lessonDay.AddHours(12))
                .IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new TheoryLessonRepository(reader).GetByIdAsync(lesson.Id, CancellationToken);
        loaded!.Attendance.Should().ContainSingle().Which.Should().Match<LessonAttendance>(mark =>
            mark.ContractId == scene.Contract.Id && mark.IsPresent && mark.Comment == "Без опоздания");
        loaded.Time.Should().Be(Hours(9, 11));
        (await new ContractRepository(reader).GetByIdAsync(scene.Contract.Id, CancellationToken))!
            .AttendedTheoryLessons.Should().Be(1);
        (await new CourseRepository(reader).IsTopicInUseAsync(scene.TopicId, CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task HasOverlap_FindsLessonsOfTheGroupOrInstructorThatAreNotCancelled()
    {
        var scene = await SceneAsync();
        var lesson = Lesson(scene, Hours(9, 11, day: 1));
        var cancelled = Lesson(scene, Hours(14, 16, day: 1));
        cancelled.Cancel("Болезнь преподавателя", Now).IsSuccess.Should().BeTrue();
        await SaveAsync(lesson, cancelled);
        await using var context = database.CreateContext();
        var lessons = new TheoryLessonRepository(context);
        var otherInstructor = Guid.CreateVersion7();

        (await lessons.HasOverlapAsync(scene.Group.Id, otherInstructor, Hours(10, 12, day: 1), null, CancellationToken))
            .Should().BeTrue();
        (await lessons.HasOverlapAsync(Guid.CreateVersion7(), scene.Instructor.Id, Hours(10, 12, day: 1), null, CancellationToken))
            .Should().BeTrue();
        (await lessons.HasOverlapAsync(scene.Group.Id, otherInstructor, Hours(10, 12, day: 1), lesson.Id, CancellationToken))
            .Should().BeFalse();
        (await lessons.HasOverlapAsync(scene.Group.Id, otherInstructor, Hours(11, 12, day: 1), null, CancellationToken))
            .Should().BeFalse();
        (await lessons.HasOverlapAsync(scene.Group.Id, otherInstructor, Hours(15, 16, day: 1), null, CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Save_OverlappingLessonOfTheGroup_IsRejectedByTheDatabase()
    {
        var scene = await SceneAsync();
        await SaveAsync(Lesson(scene, Hours(9, 11, day: 2)));

        var save = () => SaveAsync(Lesson(scene, Hours(10, 12, day: 2), Guid.CreateVersion7()));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Save_LessonOverACancelledOne_IsAccepted()
    {
        var scene = await SceneAsync();
        var cancelled = Lesson(scene, Hours(9, 11, day: 3));
        cancelled.Cancel("Перенос", Now).IsSuccess.Should().BeTrue();
        await SaveAsync(cancelled);

        var save = () => SaveAsync(Lesson(scene, Hours(9, 11, day: 3)));

        await save.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetScheduledStartingBetween_ReadsTheLessonsThatStartInTheWindow()
    {
        var scene = await SceneAsync();
        var inside = Lesson(scene, Hours(9, 10, day: 4));
        var atTheEnd = Lesson(scene, Hours(12, 13, day: 4));
        var atTheStart = Lesson(scene, Hours(7, 8, day: 4));
        await SaveAsync(inside, atTheEnd, atTheStart);
        await using var context = database.CreateContext();

        var found = await new TheoryLessonRepository(context).GetScheduledStartingBetweenAsync(
            _lessonDay.AddDays(4).AddHours(7), _lessonDay.AddDays(4).AddHours(12), CancellationToken);

        found.Select(lesson => lesson.Id).Should().BeEquivalentTo(new[] { inside.Id, atTheEnd.Id });
    }

    [Fact]
    public async Task Queries_ReadTheScheduleTheStudentsLessonsAndTheRoster()
    {
        var scene = await SceneAsync();
        var marked = Lesson(scene, Hours(9, 11, day: 5));
        var later = Lesson(scene, Hours(9, 11, day: 6));
        await SaveAsync(marked, later);
        await using (var context = database.CreateContext())
        {
            var tracked = await new TheoryLessonRepository(context).GetByIdAsync(marked.Id, CancellationToken);
            var group = await new StudyGroupRepository(context).GetByIdAsync(scene.Group.Id, CancellationToken);
            var contract = await new ContractRepository(context).GetByIdAsync(scene.Contract.Id, CancellationToken);
            tracked!.MarkAttendance(group!, contract!, false, null, scene.Instructor.Id, _lessonDay.AddDays(5).AddHours(12))
                .IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var queries = new TheoryLessonQueries(reader);
        var week = TimeInterval.Create(_lessonDay.AddDays(5), _lessonDay.AddDays(7)).Value;

        var schedule = await queries.GetScheduleAsync(
            new TheoryScheduleFilter(week, scene.Instructor.Id, null, null, null), DataScope.SchoolWide, CancellationToken);
        var ofStudent = await queries.GetOfStudentAsync(scene.Student.Id, week, CancellationToken);
        var ofGroup = await queries.GetOfGroupAsync(scene.Group.Id, CancellationToken);
        var details = await queries.GetDetailsAsync(later.Id, CancellationToken);

        schedule.Select(item => item.Id).Should().Equal(marked.Id, later.Id);
        schedule[0].TopicTitle.Should().Be("Дорожные знаки");
        schedule[0].InstructorName.Should().Be("Преподавателев Иван Петрович");
        ofStudent.Select(item => (item.Lesson.Id, item.IsPresent)).Should().Equal((marked.Id, false), (later.Id, (bool?)null));
        ofGroup!.TheoryInstructorId.Should().Be(scene.Instructor.Id);
        ofGroup.Lessons.Should().HaveCount(2);
        details!.Participants.Should().ContainSingle().Which.Should().Match<LessonParticipantItem>(participant =>
            participant.ContractId == scene.Contract.Id && participant.IsPresent == null);
        (await queries.GetOfGroupAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    private sealed record Scene(User Instructor, User Student, Course Course, Guid TopicId, Contract Contract, StudyGroup Group);
}

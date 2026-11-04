using AutoElite.Application.TheoryLessons;
using AutoElite.Application.Users;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TheoryLessons;

/// <summary>A group with an instructor, a course with topics, contracts and lessons for the theory lesson tests.</summary>
internal sealed class TheoryLessonFixture
{
    public static readonly DateTime LessonStart = TestData.Now.AddDays(2);

    public TheoryLessonFixture()
    {
        Roles = new FakeUserRoleRepository(Users);
        TopicId = Course.AddTopic("Road signs", null).Value.Id;
        Courses.Seed(Course);

        Instructor = SeedInstructor("teacher@example.com");
        Group = StudyGroup.Create("B-2026-03", Course, Instructor.UserId!.Value, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        Groups.Seed(Group);
    }

    public FakeTheoryLessonRepository Lessons { get; } = new();

    public FakeStudyGroupRepository Groups { get; } = new();

    public InMemoryRepository<Course> Courses { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public FakeUserRepository Users { get; } = new();

    public FakeUserRoleRepository Roles { get; }

    public Course Course { get; } = TestData.NewCourse();

    public Guid TopicId { get; }

    public FakeCurrentUser Instructor { get; }

    public StudyGroup Group { get; }

    public TheoryLessonAccess CreateLessonAccess(FakeCurrentUser caller) => new(Lessons, Groups, caller);

    public LessonInstructors CreateLessonInstructors() => new(new StaffMembers(Users, Roles));

    public FakeCurrentUser SeedInstructor(string email, UserRole role = UserRole.TheoryInstructor)
    {
        var user = TestData.NewUser(email);
        Users.Seed(user);
        Roles.Seed(user.Id, role);

        return new FakeCurrentUser { UserId = user.Id, Roles = new HashSet<UserRole> { role } };
    }

    public Contract SeedMember()
    {
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", TestData.NewUser($"student-{Contracts.Items.Count}@example.com"), Course, Course.Price,
                Transmission.Manual, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        Contracts.Seed(contract);
        Group.Enroll(contract, TestData.Now);

        return contract;
    }

    public TheoryLesson SeedLesson(DateTime? start = null)
    {
        var lessonStart = start ?? LessonStart;
        var lesson = TheoryLesson.Schedule(
                Group, Course, TopicId, Instructor.UserId!.Value, TimeInterval.Create(lessonStart, lessonStart.AddHours(2)).Value,
                TestData.Now)
            .Value;
        Lessons.Seed(lesson);

        return lesson;
    }
}

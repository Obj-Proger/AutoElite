using AutoElite.Application.Exams;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Exams;

/// <summary>A group with its responsible instructor and a theory template for the exam tests.</summary>
internal sealed class ExamFixture
{
    public static readonly DateTime ExamTime = TestData.Now.AddDays(7);

    public ExamFixture()
    {
        Group = StudyGroup.Create(
                "B-2026-03", Course, Instructor.UserId!.Value, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        Groups.Seed(Group);
        Template = TestTemplate.Create(
                "Exam B", LicenseCategory.B, TestRules.TrafficPoliceExam, [TestBlock.Create(20, []).Value], null)
            .Value;
        Templates.Seed(Template);
    }

    public InMemoryRepository<Exam> Exams { get; } = new();

    public FakeStudyGroupRepository Groups { get; } = new();

    public InMemoryRepository<TestTemplate> Templates { get; } = new();

    public Course Course { get; } = TestData.NewCourse();

    public FakeCurrentUser Instructor { get; } = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);

    public StudyGroup Group { get; }

    public TestTemplate Template { get; }

    public ExamAccess CreateAccess(FakeCurrentUser caller) => new(Exams, Groups, caller);

    public Exam SeedExam(ExamKind kind = ExamKind.Theory)
    {
        var exam = Exam.Schedule(Group, kind, kind == ExamKind.Theory ? Template : null, ExamTime, TestData.Now).Value;
        Exams.Seed(exam);

        return exam;
    }
}

using AutoElite.Application.Exams;
using AutoElite.Application.TestSessions;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Exams;

/// <summary>
/// A group with its responsible instructor, a member with a contract, a small theory template and a question bank
/// for the exam tests.
/// </summary>
internal sealed class ExamFixture
{
    public static readonly DateTime ExamTime = TestData.Now.AddDays(7);

    public ExamFixture()
    {
        Group = StudyGroup.Create(
                "B-2026-03", Course, Instructor.UserId!.Value, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        Groups.Seed(Group);

        // Two questions from all topics, no mistakes allowed.
        var rules = TestRules.Create(2, TimeSpan.FromMinutes(10), 0, null, 0, TimeSpan.Zero).Value;
        Template = TestTemplate.Create("Exam B", LicenseCategory.B, rules, [TestBlock.Create(2, []).Value], null).Value;
        Templates.Seed(Template);

        var topicId = Course.AddTopic("Road signs", null).Value.Id;
        for (var index = 0; index < 3; index++)
        {
            Picker.Bank.Add(Question.Create(
                    $"Question {index}", null, null, topicId, [LicenseCategory.B], QuestionSource.School, null,
                    [new AnswerOptionDraft("Right", true), new AnswerOptionDraft("Wrong", false)])
                .Value);
        }

        Member = SeedContract("member@example.com");
        Group.Enroll(Member, TestData.Now);
        Student = new FakeCurrentUser { UserId = Member.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };
    }

    public InMemoryRepository<Exam> Exams { get; } = new();

    public FakeStudyGroupRepository Groups { get; } = new();

    public InMemoryRepository<TestTemplate> Templates { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public FakeTestSessionRepository Sessions { get; } = new();

    public FakeQuestionPicker Picker { get; } = new();

    public Course Course { get; } = TestData.NewCourse();

    public FakeCurrentUser Instructor { get; } = FakeCurrentUser.SignedIn(UserRole.TheoryInstructor);

    public StudyGroup Group { get; }

    public TestTemplate Template { get; }

    public Contract Member { get; }

    public FakeCurrentUser Student { get; }

    public ExamAccess CreateAccess(FakeCurrentUser caller) => new(Exams, Groups, caller);

    public TestAttempts CreateAttempts(TimeProvider clock) => new(Sessions, new SessionQuestions(Picker), clock);

    public Contract SeedContract(string email)
    {
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", TestData.NewUser(email), Course, Course.Price, Transmission.Manual, null,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        Contracts.Seed(contract);

        return contract;
    }

    public Exam SeedExam(ExamKind kind = ExamKind.Theory)
    {
        var exam = Exam.Schedule(Group, kind, kind == ExamKind.Theory ? Template : null, ExamTime, TestData.Now).Value;
        Exams.Seed(exam);

        return exam;
    }
}

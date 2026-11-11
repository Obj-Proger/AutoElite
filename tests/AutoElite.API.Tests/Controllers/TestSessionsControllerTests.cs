using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A practice test and a theory exam taken by a student.</summary>
[Collection(nameof(ApiCollection))]
public sealed class TestSessionsControllerTests(ApiFactory api)
{
    private const string CorrectAnswer = "Верно";

    private static readonly LicenseCategory[] _categoryB = [LicenseCategory.B];

    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    // A course with a topic of two questions and a template that asks both of them, and a contract of the student.
    private static async Task<(Guid CourseId, Guid TemplateId, Guid ContractId)> PrepareAsync(HttpClient admin, Guid studentId)
    {
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var topicId = await admin.CreateAsync($"/api/v1/courses/{courseId}/topics", new { Title = "Перекрёстки" });
        foreach (var text in new[] { "Кто проезжает первым?", "Можно ли развернуться?" })
        {
            await admin.CreateAsync("/api/v1/questions", new
            {
                Text = text,
                TopicId = topicId,
                Categories = _categoryB,
                Source = QuestionSource.School,
                Options = new[] { new { Text = CorrectAnswer, IsCorrect = true }, new { Text = "Неверно", IsCorrect = false } }
            });
        }

        var templateId = await admin.CreateAsync("/api/v1/test-templates", new
        {
            Name = $"Перекрёстки {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            QuestionCount = 2,
            TimeLimitMinutes = 10,
            MaxMistakes = 1,
            ExtraQuestionsPerMistake = 0,
            ExtraMinutesPerMistake = 0,
            Blocks = new[] { new { QuestionCount = 2, TopicIds = new[] { topicId } } }
        });
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });

        return (courseId, templateId, contractId);
    }

    [Fact]
    public async Task Student_PassesAPracticeTest_AndSeesTheCorrectAnswersAfterIt()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (_, templateId, contractId) = await PrepareAsync(admin, studentUser.UserId);

        using var started = await student.SendJsonAsync(
            HttpMethod.Post, "/api/v1/test-sessions", new { ContractId = contractId, TemplateId = templateId });
        var sessionId = (await started.ReadJsonAsync()).GetProperty("id").GetGuid();
        var session = $"/api/v1/test-sessions/{sessionId}";
        var inProgress = await student.GetJsonAsync(session);
        var statuses = new List<string?>();
        foreach (var question in inProgress.GetProperty("questions").EnumerateArray())
        {
            var optionId = question.GetProperty("options").EnumerateArray()
                .Single(option => option.GetProperty("text").GetString() == CorrectAnswer)
                .GetProperty("optionId").GetGuid();
            var answer = new { SessionQuestionId = question.GetProperty("id").GetGuid(), OptionId = optionId };
            using var answered = await student.SendJsonAsync(HttpMethod.Post, $"{session}/answers", answer);
            answered.StatusCode.Should().Be(HttpStatusCode.OK);
            statuses.Add((await answered.ReadJsonAsync()).GetProperty("status").GetString());
        }

        var finished = await student.GetJsonAsync(session);
        var history = await admin.GetJsonAsync($"/api/v1/contracts/{contractId}/test-sessions");

        started.StatusCode.Should().Be(HttpStatusCode.Created);
        started.Headers.Location!.AbsolutePath.Should().Be(session);
        inProgress.GetProperty("session").GetProperty("status").GetString().Should().Be(nameof(TestSessionStatus.InProgress));
        inProgress.GetProperty("questions").EnumerateArray()
            .Should().HaveCount(2)
            .And.OnlyContain(question => question.GetProperty("correctOptionId").ValueKind == JsonValueKind.Null);
        statuses.Should().Equal(nameof(TestSessionStatus.InProgress), nameof(TestSessionStatus.Passed));
        finished.GetProperty("session").GetProperty("status").GetString().Should().Be(nameof(TestSessionStatus.Passed));
        finished.GetProperty("session").GetProperty("correctAnswerCount").GetInt32().Should().Be(2);
        finished.GetProperty("questions").EnumerateArray()
            .Should().OnlyContain(question => question.GetProperty("isCorrect").GetBoolean());
        history.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(sessionId);
    }

    [Fact]
    public async Task Student_CannotStartATheoryExamBeforeItBegins()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var instructorClient = instructor;
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (courseId, templateId, contractId) = await PrepareAsync(admin, studentUser.UserId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var groupId = await admin.CreateAsync("/api/v1/study-groups", new
        {
            Name = $"Группа {Guid.NewGuid():N}",
            CourseId = courseId,
            TheoryInstructorId = instructorUser.UserId,
            StartDate = today,
            EndDate = today.AddMonths(3),
            Capacity = 10
        });
        await admin.CreateAsync($"/api/v1/study-groups/{groupId}/members", new { ContractId = contractId });
        var examId = await instructor.CreateAsync("/api/v1/exams", new
        {
            GroupId = groupId,
            Kind = ExamKind.Theory,
            ScheduledAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(9),
            TestTemplateId = templateId
        });

        using var early = await student.SendJsonAsync(
            HttpMethod.Post, $"/api/v1/exams/{examId}/test-session", new { ContractId = contractId });

        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await early.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Exam.NotStarted");
    }
}

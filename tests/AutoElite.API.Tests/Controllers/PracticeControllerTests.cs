using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A student practising the questions of their category: by topic, as a marathon and on their mistakes.</summary>
[Collection(nameof(ApiCollection))]
public sealed class PracticeControllerTests(ApiFactory api)
{
    private const string CorrectAnswer = "Верно";

    private static readonly LicenseCategory[] _categoryB = [LicenseCategory.B];

    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    // A course with a topic of three questions and a contract of the student on it.
    private static async Task<(Guid TopicId, Guid ContractId)> PrepareAsync(HttpClient admin, Guid studentId)
    {
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var topicId = await admin.CreateAsync(
            $"/api/v1/courses/{courseId}/topics", new { Title = $"Знаки {Guid.NewGuid():N}" });
        foreach (var text in new[] { "Что означает знак?", "Можно ли остановиться?", "Кто уступает?" })
        {
            await admin.CreateAsync("/api/v1/questions", new
            {
                Text = text,
                Explanation = "Так сказано в правилах.",
                TopicId = topicId,
                Categories = _categoryB,
                Source = QuestionSource.School,
                Options = new[] { new { Text = CorrectAnswer, IsCorrect = true }, new { Text = "Неверно", IsCorrect = false } }
            });
        }

        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });

        return (topicId, contractId);
    }

    private static Guid OptionOf(JsonElement question, bool correct) =>
        question.GetProperty("options").EnumerateArray()
            .Single(option => (option.GetProperty("text").GetString() == CorrectAnswer) == correct)
            .GetProperty("id").GetGuid();

    [Fact]
    public async Task Student_PractisesATopic_AndThenTheirMistakesUntilTheyAnswerThemRight()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (topicId, contractId) = await PrepareAsync(admin, studentUser.UserId);
        var topic = $"/api/v1/practice/questions?contractId={contractId}&mode=Topic&topicId={topicId}&seed=5&offset=0&count=50";

        var questions = await student.GetJsonAsync(topic);
        var first = questions.GetProperty("questions")[0];
        var answer = new { ContractId = contractId, QuestionId = first.GetProperty("id").GetGuid(), OptionId = OptionOf(first, false) };
        using var wrong = await student.SendJsonAsync(HttpMethod.Post, "/api/v1/practice/answers", answer);
        var wrongResult = await wrong.ReadJsonAsync();
        var mistakesUri = $"/api/v1/practice/questions?contractId={contractId}&mode=Mistakes&seed=1&offset=0&count=50";
        var mistakes = await student.GetJsonAsync(mistakesUri);
        var overview = await student.GetJsonAsync($"/api/v1/practice?contractId={contractId}");
        using var right = await student.SendJsonAsync(
            HttpMethod.Post, "/api/v1/practice/answers", answer with { OptionId = OptionOf(first, true) });
        var mistakesAfter = await student.GetJsonAsync(mistakesUri);

        questions.GetProperty("total").GetInt32().Should().Be(3);
        questions.GetProperty("questions").EnumerateArray().Should().HaveCount(3);
        questions.GetProperty("questions").EnumerateArray()
            .Any(question => question.TryGetProperty("correctOptionId", out _)).Should().BeFalse();
        wrong.StatusCode.Should().Be(HttpStatusCode.OK);
        wrongResult.GetProperty("isCorrect").GetBoolean().Should().BeFalse();
        wrongResult.GetProperty("correctOptionId").GetGuid().Should().Be(OptionOf(first, true));
        wrongResult.GetProperty("explanation").GetString().Should().Be("Так сказано в правилах.");
        mistakes.GetProperty("questions").EnumerateArray().Select(question => question.GetProperty("id").GetGuid())
            .Should().Equal(answer.QuestionId);
        var practisedTopic = overview.GetProperty("topics").EnumerateArray()
            .Single(item => item.GetProperty("topicId").GetGuid() == topicId);
        practisedTopic.GetProperty("questionCount").GetInt32().Should().Be(3);
        practisedTopic.GetProperty("answeredCount").GetInt32().Should().Be(1);
        practisedTopic.GetProperty("mistakeCount").GetInt32().Should().Be(1);
        right.StatusCode.Should().Be(HttpStatusCode.OK);
        mistakesAfter.GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Marathon_ReadInParts_FollowsOneOrder()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (_, contractId) = await PrepareAsync(admin, studentUser.UserId);
        var marathon = $"/api/v1/practice/questions?contractId={contractId}&mode=Marathon&seed=9";

        var whole = await student.GetJsonAsync($"{marathon}&offset=0&count=50");
        var firstPart = await student.GetJsonAsync($"{marathon}&offset=0&count=2");
        var secondPart = await student.GetJsonAsync($"{marathon}&offset=2&count=2");

        static IEnumerable<Guid> IdsOf(JsonElement page) =>
            page.GetProperty("questions").EnumerateArray().Select(question => question.GetProperty("id").GetGuid());
        // Other tests add questions of the category too, so the parts are compared with the start of the whole.
        IdsOf(firstPart).Concat(IdsOf(secondPart)).Should().Equal(IdsOf(whole).Take(4));
        firstPart.GetProperty("total").GetInt32().Should().Be(whole.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Practice_OnTheContractOfAnotherStudent_IsNotFound()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (ownerClient, owner) = await api.SignInAsync(UserRole.Student);
        using var ownerHttp = ownerClient;
        using var stranger = await api.SignedInClientAsync(UserRole.Student);
        var (_, contractId) = await PrepareAsync(admin, owner.UserId);

        using var response = await stranger.GetAsync($"/api/v1/practice?contractId={contractId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Topic_WithoutTheTopic_IsABadRequest()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (_, contractId) = await PrepareAsync(admin, studentUser.UserId);

        using var response = await student.GetAsync(
            $"/api/v1/practice/questions?contractId={contractId}&mode=Topic&seed=1&offset=0&count=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

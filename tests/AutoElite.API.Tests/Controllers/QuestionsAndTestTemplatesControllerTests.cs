using System.Net;
using System.Text;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The question bank, written by hand and imported from a file, and the templates of the tests.</summary>
[Collection(nameof(ApiCollection))]
public sealed class QuestionsAndTestTemplatesControllerTests(ApiFactory api)
{
    private static readonly LicenseCategory[] _categoryB = [LicenseCategory.B];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<Guid> CreateTopicAsync(HttpClient admin)
    {
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });

        return await admin.CreateAsync($"/api/v1/courses/{courseId}/topics", new { Title = "Дорожные знаки" });
    }

    private static async Task<HttpResponseMessage> ImportAsync(
        HttpClient client, string? fileName, string? content, Guid topicId, QuestionSource source)
    {
        using var form = new MultipartFormDataContent();
        if (fileName is not null)
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content ?? string.Empty)), "file", fileName);

        form.Add(new StringContent(topicId.ToString()), "topicId");
        form.Add(new StringContent(Enum.GetName(source)!), "source");

        return await client.PostAsync("/api/v1/questions/import", form, CancellationToken);
    }

    [Fact]
    public async Task Instructor_WritesASchoolQuestion_ButNotAnOfficialOne()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        using var instructor = await api.SignedInClientAsync(UserRole.TheoryInstructor);
        var topicId = await CreateTopicAsync(admin);
        var question = new
        {
            Text = "Что означает этот знак?",
            Explanation = "Знак запрещает въезд.",
            TopicId = topicId,
            Categories = _categoryB,
            Source = QuestionSource.School,
            Options = new[]
            {
                new { Text = "Въезд запрещён", IsCorrect = true },
                new { Text = "Стоянка", IsCorrect = false }
            }
        };

        var questionId = await instructor.CreateAsync("/api/v1/questions", question);
        var questions = $"/api/v1/questions/{questionId}";
        using var updated = await instructor.SendJsonAsync(HttpMethod.Put, questions, new
        {
            Text = "Что означает знак 3.1?",
            question.Explanation,
            question.TopicId,
            question.Categories,
            question.Options
        });
        using var deactivated = await instructor.SendJsonAsync(HttpMethod.Post, $"{questions}/deactivate");
        var read = await instructor.GetJsonAsync(questions);
        var inactive = await instructor.GetJsonAsync($"/api/v1/questions?topicId={topicId}&isActive=false");
        using var official = await instructor.SendJsonAsync(
            HttpMethod.Post, "/api/v1/questions", question with { Source = QuestionSource.Official });

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.GetProperty("text").GetString().Should().Be("Что означает знак 3.1?");
        read.GetProperty("isActive").GetBoolean().Should().BeFalse();
        read.GetProperty("options").EnumerateArray().Select(option => option.GetProperty("isCorrect").GetBoolean())
            .Should().Equal(true, false);
        inactive.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(questionId);
        official.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await official.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Question.OfficialAdministratorOnly");
    }

    [Fact]
    public async Task Administrator_ImportsQuestionsFromATable_AndAnotherEditionUpdatesThem()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var topicId = await CreateTopicAsync(admin);
        var code = Guid.NewGuid().ToString("N")[..8];
        var table = $"""
            code,question,option1,option2,correct,categories
            {code}-1,Можно ли обгонять?,Да,Нет,2,B
            {code}-2,Кто уступает дорогу?,Трамвай,Автомобиль,2,B
            """;

        using var imported = await ImportAsync(admin, "questions.csv", table, topicId, QuestionSource.Official);
        var nextEdition = table.Replace("Можно ли", "Разрешён ли", StringComparison.Ordinal);
        using var reimported = await ImportAsync(admin, "questions.csv", nextEdition, topicId, QuestionSource.Official);
        var bank = await admin.GetJsonAsync($"/api/v1/questions?topicId={topicId}&source=Official");

        imported.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await imported.ReadJsonAsync();
        first.GetProperty("created").GetInt32().Should().Be(2);
        first.GetProperty("updated").GetInt32().Should().Be(0);
        reimported.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reimported.ReadJsonAsync()).GetProperty("updated").GetInt32().Should().Be(2);
        bank.GetProperty("totalCount").GetInt32().Should().Be(2);
        bank.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("text").GetString())
            .Should().Contain("Разрешён ли обгонять?");
    }

    [Fact]
    public async Task Import_WithoutAFile_IsRefused()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var topicId = await CreateTopicAsync(admin);

        using var response = await ImportAsync(admin, null, null, topicId, QuestionSource.School);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadJsonAsync();
        problem.GetProperty("code").GetString().Should().Be("Validation.Failed");
        problem.GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("code").GetString())
            .Should().Equal("Question.ImportContentMissing", "Document.FileNameEmpty");
    }

    [Fact]
    public async Task Instructor_CreatesATemplate_WhichStudentsSeeWhileItIsActive()
    {
        using var instructor = await api.SignedInClientAsync(UserRole.TheoryInstructor);
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var name = $"Билет {Guid.NewGuid():N}";
        var template = new
        {
            Name = name,
            Category = LicenseCategory.B,
            QuestionCount = 20,
            TimeLimitMinutes = 20,
            MaxMistakes = 2,
            MaxMistakesPerBlock = (int?)null,
            ExtraQuestionsPerMistake = 5,
            ExtraMinutesPerMistake = 5,
            Blocks = new[] { new { QuestionCount = 20, TopicIds = Array.Empty<Guid>() } }
        };

        var templateId = await instructor.CreateAsync("/api/v1/test-templates", template);
        var templates = $"/api/v1/test-templates/{templateId}";
        using var updated = await instructor.SendJsonAsync(HttpMethod.Put, templates, template with { TimeLimitMinutes = 25 });
        var read = await student.GetJsonAsync(templates);
        var offered = await student.GetJsonAsync("/api/v1/test-templates?category=B");
        using var deactivated = await instructor.SendJsonAsync(HttpMethod.Post, $"{templates}/deactivate");
        using var hidden = await student.GetAsync(templates, CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.GetProperty("name").GetString().Should().Be(name);
        read.GetProperty("timeLimitMinutes").GetInt32().Should().Be(25);
        read.GetProperty("blocks").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("questionCount").GetInt32().Should().Be(20);
        offered.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Contain(templateId);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

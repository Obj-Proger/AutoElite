using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Scenarios;

/// <summary>
/// The way of a student through the school, as the people of the school go it through the API: from the request on the
/// public form to the paid contract, the study group, the theory schedule and a passed practice test, all on the card
/// of the student.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class StudentJourneyTests(ApiFactory api)
{
    private const string CorrectAnswer = "Верно";

    private static readonly LicenseCategory[] _categoryB = [LicenseCategory.B];

    private static List<string?> TypesOf(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("type").GetString())];

    [Fact]
    public async Task FromThePublicRequestToAPassedPracticeTest()
    {
        using var anonymous = api.CreateClient();
        using var manager = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var instructorClient = instructor;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var phone = "+7999" + Random.Shared.Next(1_000_000, 9_999_999).ToString(CultureInfo.InvariantCulture);
        var email = $"{Guid.NewGuid():N}@example.com";

        // The school: a course with a topic of two questions, and a practice test of both.
        var courseId = await manager.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var topicId = await manager.CreateAsync($"/api/v1/courses/{courseId}/topics", new { Title = "Перекрёстки" });
        foreach (var text in new[] { "Кто проезжает первым?", "Можно ли развернуться?" })
        {
            await manager.CreateAsync("/api/v1/questions", new
            {
                Text = text,
                TopicId = topicId,
                Categories = _categoryB,
                Source = QuestionSource.School,
                Options = new[] { new { Text = CorrectAnswer, IsCorrect = true }, new { Text = "Неверно", IsCorrect = false } }
            });
        }

        var templateId = await manager.CreateAsync("/api/v1/test-templates", new
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

        // A visitor leaves a request; the manager works it and converts it into the contract of a new student.
        var publicRequest = new { ContactName = "Мария Белова", Phone = phone, DesiredCategory = LicenseCategory.B };
        using var request = await anonymous.SendJsonAsync(HttpMethod.Post, "/api/v1/leads/requests", publicRequest);
        var leads = await manager.GetJsonAsync($"/api/v1/leads?search={Uri.EscapeDataString(phone)}");
        var leadId = leads.GetProperty("items").EnumerateArray().Should().ContainSingle().Which.GetProperty("id").GetGuid();
        using var started = await manager.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/start-work");
        using var converted = await manager.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/conversion", new
        {
            LastName = "Белова",
            FirstName = "Мария",
            Email = email,
            PreferredLanguage = Language.Russian,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = today,
            EndDate = today.AddMonths(4)
        });
        var conversion = await converted.ReadJsonAsync();
        var studentId = conversion.GetProperty("studentId").GetGuid();
        var contractId = conversion.GetProperty("contractId").GetGuid();

        request.StatusCode.Should().Be(HttpStatusCode.NoContent);
        started.StatusCode.Should().Be(HttpStatusCode.NoContent);
        converted.StatusCode.Should().Be(HttpStatusCode.Created);

        // The student accepts the invitation; the manager forms a group and enrolls the student; the student pays.
        using var student = await api.SignInRegisteredAsync(studentId, email);
        var groupId = await manager.CreateAsync("/api/v1/study-groups", new
        {
            Name = $"Группа {Guid.NewGuid():N}",
            CourseId = courseId,
            TheoryInstructorId = instructorUser.UserId,
            StartDate = today,
            EndDate = today.AddMonths(3),
            Capacity = 10
        });
        await manager.CreateAsync($"/api/v1/study-groups/{groupId}/members", new { ContractId = contractId });
        await manager.CreateAsync(
            $"/api/v1/contracts/{contractId}/payments",
            new { Amount = 45000m, Method = PaymentMethod.Cash, Purpose = "Оплата обучения", ConfirmNow = true });

        // The instructor schedules the first lesson, which the student sees with the confirmation of the payment.
        var start = DateTime.UtcNow.Date.AddDays(2).AddHours(18);
        var lessonId = await instructor.CreateAsync(
            "/api/v1/theory-lessons", new { GroupId = groupId, TopicId = topicId, StartUtc = start, EndUtc = start.AddHours(2) });
        var period = string.Create(
            CultureInfo.InvariantCulture, $"fromUtc={DateTime.UtcNow.Date:O}&toUtc={DateTime.UtcNow.Date.AddDays(7):O}");
        var schedule = await student.GetJsonAsync($"/api/v1/me/theory-lessons?{period}");
        var notifications = await student.WaitForJsonAsync(
            "/api/v1/notifications",
            page => TypesOf(page).Contains(nameof(NotificationType.PaymentConfirmed))
                && TypesOf(page).Contains(nameof(NotificationType.EnrolledInGroup)));

        schedule.EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("lesson").GetProperty("id").GetGuid().Should().Be(lessonId);
        TypesOf(notifications).Should().Contain(
            new[] { nameof(NotificationType.EnrolledInGroup), nameof(NotificationType.PaymentConfirmed) });

        // The student passes a practice test.
        var sessionId = await student.CreateAsync("/api/v1/test-sessions", new { ContractId = contractId, TemplateId = templateId });
        var session = await student.GetJsonAsync($"/api/v1/test-sessions/{sessionId}");
        foreach (var question in session.GetProperty("questions").EnumerateArray())
        {
            var optionId = question.GetProperty("options").EnumerateArray()
                .Single(option => option.GetProperty("text").GetString() == CorrectAnswer)
                .GetProperty("optionId").GetGuid();
            using var answered = await student.SendJsonAsync(
                HttpMethod.Post,
                $"/api/v1/test-sessions/{sessionId}/answers",
                new { SessionQuestionId = question.GetProperty("id").GetGuid(), OptionId = optionId });
            answered.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // The card of the student shows it all.
        var card = await student.GetJsonAsync($"/api/v1/students/{studentId}/card");

        var contract = card.GetProperty("contracts").EnumerateArray().Should().ContainSingle().Which;
        contract.GetProperty("contract").GetProperty("id").GetGuid().Should().Be(contractId);
        contract.GetProperty("contract").GetProperty("paidAmount").GetDecimal().Should().Be(45000m);
        contract.GetProperty("contract").GetProperty("debtAmount").GetDecimal().Should().Be(0m);
        contract.GetProperty("group").GetProperty("groupId").GetGuid().Should().Be(groupId);
        contract.GetProperty("tests").GetProperty("passed").GetInt32().Should().Be(1);
    }
}

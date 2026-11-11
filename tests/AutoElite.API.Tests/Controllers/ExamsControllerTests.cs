using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>An internal exam of a study group, from its scheduling to the list of its students.</summary>
[Collection(nameof(ApiCollection))]
public sealed class ExamsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Instructor_SchedulesAPracticeExam_WhichTheGroupAndItsStudentsSee()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (theoryInstructor, theoryInstructorUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var theoryInstructorClient = theoryInstructor;
        using var drivingInstructor = await api.SignedInClientAsync(UserRole.DrivingInstructor);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var groupId = await admin.CreateAsync("/api/v1/study-groups", new
        {
            Name = $"Группа {Guid.NewGuid():N}",
            CourseId = courseId,
            TheoryInstructorId = theoryInstructorUser.UserId,
            StartDate = Today,
            EndDate = Today.AddMonths(3),
            Capacity = 10
        });
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentUser.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });
        await admin.CreateAsync($"/api/v1/study-groups/{groupId}/members", new { ContractId = contractId });
        var scheduledAt = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        var period = string.Create(
            CultureInfo.InvariantCulture, $"fromUtc={DateTime.UtcNow.Date:O}&toUtc={DateTime.UtcNow.Date.AddDays(7):O}");

        using var withoutTemplate = await theoryInstructor.SendJsonAsync(
            HttpMethod.Post, "/api/v1/exams", new { GroupId = groupId, Kind = ExamKind.Theory, ScheduledAtUtc = scheduledAt });
        var examId = await theoryInstructor.CreateAsync(
            "/api/v1/exams", new { GroupId = groupId, Kind = ExamKind.Practice, ScheduledAtUtc = scheduledAt });
        var exam = $"/api/v1/exams/{examId}";
        using var moved = await theoryInstructor.SendJsonAsync(
            HttpMethod.Put, $"{exam}/time", new { ScheduledAtUtc = scheduledAt.AddDays(1) });
        using var early = await drivingInstructor.SendJsonAsync(
            HttpMethod.Post, $"{exam}/results", new { ContractId = contractId, Passed = true });
        var details = await admin.GetJsonAsync(exam);
        var ofGroup = await drivingInstructor.GetJsonAsync($"/api/v1/study-groups/{groupId}/exams");
        var schedule = await theoryInstructor.GetJsonAsync($"/api/v1/exams?{period}&kind=Practice&groupId={groupId}");
        var own = await student.GetJsonAsync("/api/v1/me/exams");

        withoutTemplate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await withoutTemplate.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Exam.TemplateRequired");
        moved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await early.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Exam.NotStarted");
        details.GetProperty("exam").GetProperty("kind").GetString().Should().Be(nameof(ExamKind.Practice));
        details.GetProperty("exam").GetProperty("scheduledAtUtc").GetDateTime().Should().Be(scheduledAt.AddDays(1));
        details.GetProperty("results").EnumerateArray().Should().BeEmpty();
        ofGroup.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(examId);
        schedule.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(examId);
        var studentExam = own.EnumerateArray().Should().ContainSingle().Which;
        studentExam.GetProperty("exam").GetProperty("id").GetGuid().Should().Be(examId);
        studentExam.GetProperty("result").ValueKind.Should().Be(JsonValueKind.Null);

        using var cancelled = await theoryInstructor.SendJsonAsync(
            HttpMethod.Post, $"{exam}/cancel", new { Reason = "Нет площадки" });
        var cancelledExam = await admin.GetJsonAsync(exam);

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        cancelledExam.GetProperty("exam").GetProperty("status").GetString().Should().Be(nameof(ExamStatus.Cancelled));
        cancelledExam.GetProperty("exam").GetProperty("cancellationReason").GetString().Should().Be("Нет площадки");
    }

    [Fact]
    public async Task Student_CannotReadTheExamSchedule()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var period = string.Create(
            CultureInfo.InvariantCulture, $"fromUtc={DateTime.UtcNow.Date:O}&toUtc={DateTime.UtcNow.Date.AddDays(7):O}");

        using var response = await student.GetAsync($"/api/v1/exams?{period}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

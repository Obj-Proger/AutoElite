using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A study group from its forming to the theory lessons its students attend.</summary>
[Collection(nameof(ApiCollection))]
public sealed class StudyGroupsAndTheoryLessonsControllerTests(ApiFactory api)
{
    // A contract start in the past and a group that starts today: the dates the school works with.
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<Guid> CreateCourseAsync(HttpClient admin) =>
        await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });

    private static async Task<Guid> CreateGroupAsync(HttpClient admin, Guid courseId, Guid instructorId) =>
        await admin.CreateAsync("/api/v1/study-groups", new
        {
            Name = $"Группа {Guid.NewGuid():N}",
            CourseId = courseId,
            TheoryInstructorId = instructorId,
            StartDate = Today,
            EndDate = Today.AddMonths(3),
            Capacity = 10
        });

    private static async Task<Guid> DrawUpContractAsync(HttpClient admin, Guid studentId, Guid courseId) =>
        await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });

    private static string Period(DateTime fromUtc, DateTime toUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"fromUtc={fromUtc:O}&toUtc={toUtc:O}");

    [Fact]
    public async Task Manager_FormsAGroupAndEnrollsAndWithdrawsAStudent()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var instructorClient = instructor;
        var student = await api.InviteAsync(UserRole.Student);
        var courseId = await CreateCourseAsync(admin);
        var groupId = await CreateGroupAsync(admin, courseId, instructorUser.UserId);
        var contractId = await DrawUpContractAsync(admin, student.UserId, courseId);
        var group = $"/api/v1/study-groups/{groupId}";

        using var enrolled = await admin.SendJsonAsync(HttpMethod.Post, $"{group}/members", new { ContractId = contractId });
        using var updated = await admin.SendJsonAsync(HttpMethod.Put, group, new
        {
            Name = "Вечерняя группа",
            TheoryInstructorId = instructorUser.UserId,
            StartDate = Today,
            EndDate = Today.AddMonths(4),
            Capacity = 20
        });
        var own = await instructor.GetJsonAsync("/api/v1/study-groups");
        using var withdrawn = await admin.SendJsonAsync(HttpMethod.Delete, $"{group}/members/{contractId}");
        using var deactivated = await admin.SendJsonAsync(HttpMethod.Post, $"{group}/deactivate");
        var closed = await admin.GetJsonAsync(group);
        using var reactivated = await admin.SendJsonAsync(HttpMethod.Post, $"{group}/reactivate");
        var found = await admin.GetJsonAsync($"/api/v1/study-groups?courseId={courseId}&isActive=true");

        enrolled.StatusCode.Should().Be(HttpStatusCode.Created);
        enrolled.Headers.Location!.AbsolutePath.Should().Be(group);
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        own.GetProperty("totalCount").GetInt32().Should().Be(1);
        var item = own.GetProperty("items")[0];
        item.GetProperty("id").GetGuid().Should().Be(groupId);
        item.GetProperty("name").GetString().Should().Be("Вечерняя группа");
        item.GetProperty("capacity").GetInt32().Should().Be(20);
        item.GetProperty("currentMemberCount").GetInt32().Should().Be(1);
        withdrawn.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        closed.GetProperty("isActive").GetBoolean().Should().BeFalse();
        var enrollment = closed.GetProperty("enrollments").EnumerateArray().Should().ContainSingle().Which;
        enrollment.GetProperty("contractId").GetGuid().Should().Be(contractId);
        enrollment.GetProperty("withdrawnAtUtc").ValueKind.Should().Be(JsonValueKind.String);
        reactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        found.GetProperty("items").EnumerateArray().Select(listed => listed.GetProperty("id").GetGuid()).Should().Equal(groupId);
    }

    [Fact]
    public async Task Instructor_SchedulesAndChangesALesson_WhichTheStudentsOfTheGroupSee()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var instructorClient = instructor;
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var substitute = await api.InviteAsync(UserRole.TheoryInstructor);
        var courseId = await CreateCourseAsync(admin);
        var topicId = await admin.CreateAsync($"/api/v1/courses/{courseId}/topics", new { Title = "Дорожные знаки" });
        var groupId = await CreateGroupAsync(admin, courseId, instructorUser.UserId);
        var contractId = await DrawUpContractAsync(admin, studentUser.UserId, courseId);
        await admin.CreateAsync($"/api/v1/study-groups/{groupId}/members", new { ContractId = contractId });
        var start = DateTime.UtcNow.Date.AddDays(2).AddHours(9);
        var period = Period(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7));

        var lessonId = await instructor.CreateAsync(
            "/api/v1/theory-lessons", new { GroupId = groupId, TopicId = topicId, StartUtc = start, EndUtc = start.AddHours(2) });
        var lesson = $"/api/v1/theory-lessons/{lessonId}";
        using var moved = await instructor.SendJsonAsync(
            HttpMethod.Put, $"{lesson}/time", new { StartUtc = start.AddDays(1), EndUtc = start.AddDays(1).AddHours(2) });
        using var handedOver = await instructor.SendJsonAsync(
            HttpMethod.Put, $"{lesson}/instructor", new { InstructorId = substitute.UserId });
        using var early = await instructor.SendJsonAsync(
            HttpMethod.Put, $"{lesson}/attendance", new { Marks = new[] { new { ContractId = contractId, IsPresent = true } } });
        var details = await instructor.GetJsonAsync(lesson);
        var ofGroup = await admin.GetJsonAsync($"/api/v1/study-groups/{groupId}/theory-lessons");
        var schedule = await instructor.GetJsonAsync($"/api/v1/theory-lessons?{period}&groupId={groupId}");
        var studentSchedule = await student.GetJsonAsync($"/api/v1/me/theory-lessons?{period}");

        moved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        handedOver.StatusCode.Should().Be(HttpStatusCode.NoContent);
        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await early.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("TheoryLesson.NotStarted");
        details.GetProperty("instructorId").GetGuid().Should().Be(substitute.UserId);
        details.GetProperty("responsibleInstructorId").GetGuid().Should().Be(instructorUser.UserId);
        details.GetProperty("startUtc").GetDateTime().Should().Be(start.AddDays(1));
        details.GetProperty("participants").EnumerateArray().Select(participant => participant.GetProperty("contractId").GetGuid())
            .Should().Equal(contractId);
        ofGroup.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(lessonId);
        schedule.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(lessonId);
        var own = studentSchedule.EnumerateArray().Should().ContainSingle().Which;
        own.GetProperty("lesson").GetProperty("id").GetGuid().Should().Be(lessonId);
        own.GetProperty("isPresent").ValueKind.Should().Be(JsonValueKind.Null);

        using var cancelled = await instructor.SendJsonAsync(HttpMethod.Post, $"{lesson}/cancel", new { Reason = "Болезнь" });
        var cancelledLesson = await instructor.GetJsonAsync(lesson);

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        cancelledLesson.GetProperty("status").GetString().Should().Be(nameof(TheoryLessonStatus.Cancelled));
        cancelledLesson.GetProperty("cancellationReason").GetString().Should().Be("Болезнь");
    }

    [Fact]
    public async Task Student_CannotReadStudyGroups()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);

        using var response = await student.GetAsync("/api/v1/study-groups", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

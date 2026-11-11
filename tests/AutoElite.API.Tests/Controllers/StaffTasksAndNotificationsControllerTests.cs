using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A task given to a staff member, who learns about it from a notification.</summary>
[Collection(nameof(ApiCollection))]
public sealed class StaffTasksAndNotificationsControllerTests(ApiFactory api)
{
    [Fact]
    public async Task Instructor_IsNotifiedOfATask_AndWorksOnIt()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var instructorClient = instructor;
        var (colleague, colleagueUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var colleagueClient = colleague;
        var dueAt = DateTime.UtcNow.Date.AddDays(2).AddHours(12);

        var taskId = await admin.CreateAsync("/api/v1/staff-tasks", new
        {
            Title = "Проверить шины",
            AssigneeId = instructorUser.UserId,
            DueAtUtc = dueAt,
            Recurrence = TaskRecurrence.None
        });
        var task = $"/api/v1/staff-tasks/{taskId}";
        var unread = await instructor.WaitForJsonAsync(
            "/api/v1/notifications?isRead=false", page => page.GetProperty("totalCount").GetInt32() > 0);
        var own = await instructor.GetJsonAsync("/api/v1/me/staff-tasks?status=New");
        using var started = await instructor.SendJsonAsync(HttpMethod.Post, $"{task}/start");
        using var completed = await instructor.SendJsonAsync(HttpMethod.Post, $"{task}/complete");
        var done = await admin.GetJsonAsync(task);
        using var reopened = await instructor.SendJsonAsync(HttpMethod.Post, $"{task}/reopen");
        using var updated = await admin.SendJsonAsync(
            HttpMethod.Put, task, new { Title = "Проверить шины и тормоза", DueAtUtc = dueAt.AddDays(1) });
        using var reassigned = await admin.SendJsonAsync(HttpMethod.Put, $"{task}/assignee", new { AssigneeId = colleagueUser.UserId });
        var handedOver = await colleague.GetJsonAsync(task);
        var managed = await admin.GetJsonAsync($"/api/v1/staff-tasks?assigneeId={colleagueUser.UserId}&status=InProgress");

        var notification = unread.GetProperty("items").EnumerateArray().Should().ContainSingle().Which;
        notification.GetProperty("type").GetString().Should().Be(nameof(NotificationType.TaskAssigned));
        notification.GetProperty("subjectId").GetGuid().Should().Be(taskId);
        own.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(taskId);
        started.StatusCode.Should().Be(HttpStatusCode.NoContent);
        completed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        done.GetProperty("status").GetString().Should().Be(nameof(StaffTaskStatus.Done));
        reopened.StatusCode.Should().Be(HttpStatusCode.NoContent);
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reassigned.StatusCode.Should().Be(HttpStatusCode.NoContent);
        handedOver.GetProperty("title").GetString().Should().Be("Проверить шины и тормоза");
        handedOver.GetProperty("assigneeId").GetGuid().Should().Be(colleagueUser.UserId);
        managed.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(taskId);

        using var read = await instructor.SendJsonAsync(
            HttpMethod.Post, $"/api/v1/notifications/{notification.GetProperty("id").GetGuid()}/read");
        using var readAll = await instructor.SendJsonAsync(HttpMethod.Post, "/api/v1/notifications/read-all");
        var count = await instructor.GetJsonAsync("/api/v1/notifications/unread-count");

        read.StatusCode.Should().Be(HttpStatusCode.NoContent);
        readAll.StatusCode.Should().Be(HttpStatusCode.OK);
        (await readAll.ReadJsonAsync()).GetProperty("count").GetInt32().Should().Be(0);
        count.GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Student_CannotReadTheTasksOfTheStaff()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);

        using var response = await student.GetAsync("/api/v1/me/staff-tasks", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

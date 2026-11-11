using System.Net;
using System.Net.Http.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The users, the branches and the settings of the school, as administrators manage them.</summary>
[Collection(nameof(ApiCollection))]
public sealed class AdministrationControllerTests(ApiFactory api)
{
    private static readonly UserRole[] _studentRole = [UserRole.Student];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Administrator_InvitesFindsAndManagesAUser()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var email = $"{Guid.NewGuid():N}@example.com";

        using var invited = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new { LastName = "Иванов", FirstName = "Иван", Email = email, Roles = _studentRole },
            ApiJson.Options,
            CancellationToken);
        invited.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await invited.ReadJsonAsync()).GetProperty("id").GetGuid();
        invited.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/users/{userId}");

        var user = await admin.GetJsonAsync($"/api/v1/users/{userId}");
        user.GetProperty("email").GetString().Should().Be(email);
        user.GetProperty("hasAcceptedInvitation").GetBoolean().Should().BeFalse();

        using var renamed = await admin.PutAsJsonAsync(
            $"/api/v1/users/{userId}", new { LastName = "Петров", FirstName = "Пётр" }, CancellationToken);
        using var granted = await admin.PutAsync($"/api/v1/users/{userId}/roles/Manager", null, CancellationToken);
        using var revoked = await admin.DeleteAsync($"/api/v1/users/{userId}/roles/Manager", CancellationToken);
        using var deactivated = await admin.PostAsync($"/api/v1/users/{userId}/deactivate", null, CancellationToken);
        var inactive = await admin.GetJsonAsync($"/api/v1/users/{userId}");
        using var reactivated = await admin.PostAsync($"/api/v1/users/{userId}/reactivate", null, CancellationToken);
        var found = await admin.GetJsonAsync($"/api/v1/users?search={email}");

        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        granted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        inactive.GetProperty("isActive").GetBoolean().Should().BeFalse();
        inactive.GetProperty("lastName").GetString().Should().Be("Петров");
        found.GetProperty("totalCount").GetInt32().Should().Be(1);
        var item = found.GetProperty("items")[0];
        item.GetProperty("id").GetGuid().Should().Be(userId);
        item.GetProperty("isActive").GetBoolean().Should().BeTrue();
        item.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).Should().Equal(nameof(UserRole.Student));
    }

    [Fact]
    public async Task Student_CannotListUsers()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);

        using var response = await student.GetAsync("/api/v1/users", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task UnknownUser_IsNotFound()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);

        using var response = await admin.GetAsync($"/api/v1/users/{Guid.NewGuid()}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Administrator_OpensChangesAndClosesABranch_WhichStudentsCanRead()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var name = $"Филиал {Guid.NewGuid():N}";
        var branch = new { Name = name, City = "Екатеринбург", Street = "ул. Ленина, 1", TimeZoneId = "Asia/Yekaterinburg" };

        using var created = await admin.PostAsJsonAsync("/api/v1/branches", branch, CancellationToken);
        var branchId = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();
        using var updated = await admin.PutAsJsonAsync(
            $"/api/v1/branches/{branchId}", branch with { Street = "ул. Мира, 5" }, CancellationToken);
        var read = await student.GetJsonAsync($"/api/v1/branches/{branchId}");
        using var closed = await admin.PostAsync($"/api/v1/branches/{branchId}/deactivate", null, CancellationToken);
        var active = await student.GetJsonAsync("/api/v1/branches?isActive=true");
        using var refused = await student.PostAsJsonAsync("/api/v1/branches", branch, CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.GetProperty("name").GetString().Should().Be(name);
        read.GetProperty("street").GetString().Should().Be("ул. Мира, 5");
        closed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        active.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().NotContain(branchId);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrator_ChangesTheSettings()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var original = await admin.GetJsonAsync("/api/v1/settings");
        var notice = original.GetProperty("studentCancellationNoticeMinutes").GetInt32();
        var settings = new
        {
            DefaultCurrency = original.GetProperty("defaultCurrency").GetString(),
            TimeZoneId = original.GetProperty("timeZoneId").GetString(),
            QualityWindowSize = original.GetProperty("qualityWindowSize").GetInt32(),
            LowRatingThreshold = original.GetProperty("lowRatingThreshold").GetInt32(),
            StudentCancellationNoticeMinutes = notice + 60,
            LessonReminderLeadTimeMinutes = original.GetProperty("lessonReminderLeadTimeMinutes")
                .EnumerateArray().Select(minutes => minutes.GetInt32()).ToList()
        };

        using var changed = await admin.PutAsJsonAsync("/api/v1/settings", settings, CancellationToken);
        var read = await admin.GetJsonAsync("/api/v1/settings");
        using var restored = await admin.PutAsJsonAsync(
            "/api/v1/settings", settings with { StudentCancellationNoticeMinutes = notice }, CancellationToken);

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.GetProperty("studentCancellationNoticeMinutes").GetInt32().Should().Be(notice + 60);
        restored.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

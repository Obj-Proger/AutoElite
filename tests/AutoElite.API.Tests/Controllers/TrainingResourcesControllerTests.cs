using System.Net;
using System.Net.Http.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The courses, the vehicles, the training grounds and the driving routes of the school.</summary>
[Collection(nameof(ApiCollection))]
public sealed class TrainingResourcesControllerTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Administrator_BuildsTheTopicsOfACourse_WhichStudentsCanRead()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var topics = "/api/v1/courses/" + courseId + "/topics";

        using var added = await admin.PostAsJsonAsync(topics, new { Title = "Общие положения" }, CancellationToken);
        var signsId = await admin.CreateAsync(topics, new { Title = "Дорожные знаки" });
        var markingsId = await admin.CreateAsync(topics, new { Title = "Разметка" });
        using var moved = await admin.PutAsJsonAsync($"{topics}/{markingsId}/position", new { NewPosition = 1 }, CancellationToken);
        using var renamed = await admin.PutAsJsonAsync($"{topics}/{signsId}", new { Title = "Знаки" }, CancellationToken);
        var course = await student.GetJsonAsync($"/api/v1/courses/{courseId}");

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        added.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/courses/{courseId}");
        moved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        course.GetProperty("category").GetString().Should().Be("B");
        course.GetProperty("topics").EnumerateArray().Select(topic => topic.GetProperty("title").GetString())
            .Should().Equal("Разметка", "Общие положения", "Знаки");
    }

    [Fact]
    public async Task Administrator_RegistersAVehicle_WhichInstructorsButNotStudentsCanRead()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        using var instructor = await api.SignedInClientAsync(UserRole.DrivingInstructor);
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var plate = "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var vehicle = new
        {
            Plate = plate,
            Make = "Lada",
            Model = "Vesta",
            Year = 2023,
            Category = LicenseCategory.B,
            Transmission = Transmission.Manual
        };

        var vehicleId = await admin.CreateAsync("/api/v1/vehicles", vehicle);
        using var updated = await admin.PutAsJsonAsync(
            $"/api/v1/vehicles/{vehicleId}", vehicle with { Transmission = Transmission.Automatic }, ApiJson.Options, CancellationToken);
        var read = await instructor.GetJsonAsync($"/api/v1/vehicles/{vehicleId}");
        var automatic = await instructor.GetJsonAsync("/api/v1/vehicles?transmission=Automatic&isActive=true");
        using var refused = await student.GetAsync("/api/v1/vehicles", CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.GetProperty("plate").GetString().Should().Be(plate);
        read.GetProperty("transmission").GetString().Should().Be(nameof(Transmission.Automatic));
        automatic.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Contain(vehicleId);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Instructor_DescribesARouteFromATrainingGround()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var instructorClient = instructor;
        var ground = new { Name = $"Автодром {Guid.NewGuid():N}", City = "Екатеринбург", Street = "ул. Мира, 5" };
        var groundId = await admin.CreateAsync("/api/v1/training-grounds", ground);

        var routeId = await instructor.CreateAsync(
            "/api/v1/driving-routes",
            new { Name = "Круг по центру", Description = "Перекрёстки и разворот", TrainingGroundId = groundId });
        var route = await admin.GetJsonAsync($"/api/v1/driving-routes/{routeId}");
        using var closed = await admin.PostAsync($"/api/v1/driving-routes/{routeId}/deactivate", null, CancellationToken);
        var active = await instructor.GetJsonAsync($"/api/v1/driving-routes?authorId={instructorUser.UserId}&isActive=true");

        route.GetProperty("authorId").GetGuid().Should().Be(instructorUser.UserId);
        route.GetProperty("trainingGroundId").GetGuid().Should().Be(groundId);
        closed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        active.EnumerateArray().Should().BeEmpty();
    }
}

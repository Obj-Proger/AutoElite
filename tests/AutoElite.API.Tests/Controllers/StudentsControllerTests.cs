using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The progress of a student that their instructors read, without the money of the contracts.</summary>
[Collection(nameof(ApiCollection))]
public sealed class StudentsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    [Fact]
    public async Task Instructors_ReadTheProgressOfTheirStudent_WhomOthersDoNotFind()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (driving, _) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var drivingClient = driving;
        using var otherDriving = await api.SignedInClientAsync(UserRole.DrivingInstructor);
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentUser.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });
        var vehicleId = await admin.CreateAsync("/api/v1/vehicles", new
        {
            Plate = "S" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Make = "Lada",
            Model = "Vesta",
            Year = 2023,
            Category = LicenseCategory.B,
            Transmission = Transmission.Manual
        });
        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(10);
        var slotId = await driving.CreateAsync(
            "/api/v1/driving-slots", new { VehicleId = vehicleId, StartUtc = start, EndUtc = start.AddHours(2) });
        using var booked = await student.SendJsonAsync(
            HttpMethod.Post, $"/api/v1/driving-slots/{slotId}/book", new { ContractId = contractId });
        booked.EnsureSuccessStatusCode();
        var progress = $"/api/v1/students/{studentUser.UserId}/progress";

        var read = await driving.GetJsonAsync(progress);
        using var stranger = await otherDriving.GetAsync(progress, TestContext.Current.CancellationToken);
        using var own = await student.GetAsync(progress, TestContext.Current.CancellationToken);

        read.GetProperty("studentId").GetGuid().Should().Be(studentUser.UserId);
        var contract = read.GetProperty("contracts").EnumerateArray().Should().ContainSingle().Which;
        contract.GetProperty("contractId").GetGuid().Should().Be(contractId);
        contract.GetProperty("coursePracticeHours").GetInt32().Should().Be(56);
        contract.GetProperty("driving").GetProperty("upcoming").GetInt32().Should().Be(1);
        contract.TryGetProperty("debtAmount", out _).Should().BeFalse();
        stranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        own.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

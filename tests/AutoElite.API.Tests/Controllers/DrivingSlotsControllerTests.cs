using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A practice slot from its offer by an instructor to the booking of a student.</summary>
[Collection(nameof(ApiCollection))]
public sealed class DrivingSlotsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static string Period(DateTime fromUtc, DateTime toUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"fromUtc={fromUtc:O}&toUtc={toUtc:O}");

    private static async Task<Guid> CreateVehicleAsync(HttpClient admin) =>
        await admin.CreateAsync("/api/v1/vehicles", new
        {
            Plate = "D" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Make = "Lada",
            Model = "Vesta",
            Year = 2023,
            Category = LicenseCategory.B,
            Transmission = Transmission.Manual
        });

    private static async Task<Guid> DrawUpContractAsync(HttpClient admin, Guid studentId)
    {
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });

        return await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });
    }

    [Fact]
    public async Task Student_BooksAnOfferedSlotAndCancelsTheBooking()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (instructor, instructorUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var instructorClient = instructor;
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var vehicleId = await CreateVehicleAsync(admin);
        var contractId = await DrawUpContractAsync(admin, studentUser.UserId);
        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(10);
        var period = Period(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7));

        var slotId = await instructor.CreateAsync(
            "/api/v1/driving-slots", new { VehicleId = vehicleId, StartUtc = start, EndUtc = start.AddHours(2) });
        var slot = $"/api/v1/driving-slots/{slotId}";
        var available = await student.GetJsonAsync(
            $"/api/v1/driving-slots/available?contractId={contractId}&{period}&instructorId={instructorUser.UserId}");
        using var booked = await student.SendJsonAsync(HttpMethod.Post, $"{slot}/book", new { ContractId = contractId });
        var schedule = await instructor.GetJsonAsync($"/api/v1/driving-slots?{period}&status=Booked");
        var lessons = await student.GetJsonAsync($"/api/v1/me/driving-lessons?{period}");
        using var early = await instructor.SendJsonAsync(HttpMethod.Post, $"{slot}/complete", new { ActualHours = 2m });

        available.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(slotId);
        booked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var scheduled = schedule.EnumerateArray().Should().ContainSingle().Which;
        scheduled.GetProperty("slot").GetProperty("id").GetGuid().Should().Be(slotId);
        scheduled.GetProperty("booking").GetProperty("contractId").GetGuid().Should().Be(contractId);
        var lesson = lessons.EnumerateArray().Should().ContainSingle().Which;
        lesson.GetProperty("slot").GetProperty("vehicleId").GetGuid().Should().Be(vehicleId);
        lesson.GetProperty("booking").GetProperty("status").GetString().Should().Be(nameof(DrivingBookingStatus.Active));
        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await early.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("DrivingSlot.NotStarted");

        using var cancelled = await student.SendJsonAsync(HttpMethod.Post, $"{slot}/cancel-booking", new { Reason = "Заболел" });
        var reopened = await admin.GetJsonAsync(slot);
        using var withdrawn = await instructor.SendJsonAsync(
            HttpMethod.Post, $"{slot}/withdraw", new { Reason = "Ремонт машины" });
        var closed = await instructor.GetJsonAsync(slot);

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reopened.GetProperty("slot").GetProperty("status").GetString().Should().Be(nameof(DrivingSlotStatus.Available));
        var booking = reopened.GetProperty("bookings").EnumerateArray().Should().ContainSingle().Which;
        booking.GetProperty("status").GetString().Should().Be(nameof(DrivingBookingStatus.Cancelled));
        booking.GetProperty("cancellationReason").GetString().Should().Be("Заболел");
        booking.GetProperty("cancelledByUserId").GetGuid().Should().Be(studentUser.UserId);
        withdrawn.StatusCode.Should().Be(HttpStatusCode.NoContent);
        closed.GetProperty("slot").GetProperty("status").GetString().Should().Be(nameof(DrivingSlotStatus.Withdrawn));
    }

    [Fact]
    public async Task Student_CannotReadTheSchedule()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var period = Period(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7));

        using var response = await student.GetAsync($"/api/v1/driving-slots?{period}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task LessonsOfAStudent_AreEmptyWithoutBookings()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);
        var period = Period(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7));

        var lessons = await student.GetJsonAsync($"/api/v1/me/driving-lessons?{period}");

        lessons.ValueKind.Should().Be(JsonValueKind.Array);
        lessons.EnumerateArray().Should().BeEmpty();
    }
}

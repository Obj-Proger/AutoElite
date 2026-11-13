using System.Globalization;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Client.Offline;

/// <summary>The actions a student takes without a network, sent to the API once it can be reached.</summary>
[Collection(nameof(ApiCollection))]
public sealed class OfflineSynchronizerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Period(DateTime fromUtc, DateTime toUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"fromUtc={fromUtc:O}&toUtc={toUtc:O}");

    // A student with a contract, and a slot of an instructor that the contract can book.
    private async Task<(HttpClient Student, InvitedUser User, Guid ContractId, Guid SlotId)> PrepareAsync()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        using var instructor = await api.SignedInClientAsync(UserRole.DrivingInstructor);
        var (student, user) = await api.SignInAsync(UserRole.Student);
        var vehicleId = await admin.CreateAsync("/api/v1/vehicles", new
        {
            Plate = "O" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Make = "Lada",
            Model = "Vesta",
            Year = 2023,
            Category = LicenseCategory.B,
            Transmission = Transmission.Manual
        });
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
            StudentId = user.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });
        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(10);
        var slotId = await instructor.CreateAsync(
            "/api/v1/driving-slots", new { VehicleId = vehicleId, StartUtc = start, EndUtc = start.AddHours(2) });

        return (student, user, contractId, slotId);
    }

    [Fact]
    public async Task BookingTakenWithoutANetwork_IsSentWhenTheNetworkReturns()
    {
        var (student, user, contractId, slotId) = await PrepareAsync();
        using var studentClient = student;
        await using var device = new TestDevice(api);
        await device.SignInAsync(user);
        var synchronizer = device.Get<OfflineSynchronizer>();
        var queue = device.Get<OfflineQueue>();
        var booking = new BookDrivingSlot(slotId, contractId);

        device.IsOnline = false;
        var offline = await synchronizer.SubmitAsync(booking, CancellationToken);
        var waiting = await queue.ListAsync(CancellationToken);
        device.IsOnline = true;
        var online = await synchronizer.SynchronizeAsync(CancellationToken);
        var left = await queue.ListAsync(CancellationToken);
        var lessons = await student.GetJsonAsync(
            $"/api/v1/me/driving-lessons?{Period(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(7))}");

        offline.Should().Be(new SynchronizationResult(Sent: 0, Rejected: 0, IsComplete: false));
        var pending = waiting.Should().ContainSingle().Which;
        pending.Action.Should().Be(booking);
        pending.Attempts.Should().Be(1);
        pending.IsRejected.Should().BeFalse();
        online.Should().Be(new SynchronizationResult(Sent: 1, Rejected: 0, IsComplete: true));
        left.Should().BeEmpty();
        lessons.EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("slot").GetProperty("id").GetGuid().Should().Be(slotId);
    }

    [Fact]
    public async Task ActionTheApiRefuses_IsKeptAsRejectedAndTheNextOneIsSent()
    {
        var (student, user, contractId, slotId) = await PrepareAsync();
        student.Dispose();
        await using var device = new TestDevice(api);
        await device.SignInAsync(user);
        var queue = device.Get<OfflineQueue>();
        var missing = new BookDrivingSlot(Guid.NewGuid(), contractId);

        await queue.EnqueueAsync(missing, CancellationToken);
        var result = await device.Get<OfflineSynchronizer>().SubmitAsync(new BookDrivingSlot(slotId, contractId), CancellationToken);
        var pending = await queue.ListAsync(CancellationToken);

        result.Should().Be(new SynchronizationResult(Sent: 1, Rejected: 1, IsComplete: true));
        var rejected = pending.Should().ContainSingle().Which;
        rejected.Action.Should().Be(missing);
        rejected.RejectedWithStatusCode.Should().Be(404);

        (await queue.DismissAsync(rejected.Id, CancellationToken)).Should().BeTrue();
        (await queue.ListAsync(CancellationToken)).Should().BeEmpty();
    }
}

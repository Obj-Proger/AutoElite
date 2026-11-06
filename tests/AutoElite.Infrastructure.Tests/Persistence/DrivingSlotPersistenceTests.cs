using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingSlots;
using AutoElite.Domain.Common;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class DrivingSlotPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _lessonDay = new(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateTime Now => FixedClock.Default.UtcNow;

    private static TimeInterval Hours(int fromHour, int toHour, int day = 0) =>
        TimeInterval.Create(_lessonDay.AddDays(day).AddHours(fromHour), _lessonDay.AddDays(day).AddHours(toHour)).Value;

    private static Vehicle NewVehicle() =>
        Vehicle.Register(
                LicensePlate.Create("ВС" + string.Concat(Guid.NewGuid().ToString("N").Where(char.IsDigit).Take(8))).Value,
                "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, Now)
            .Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    /// <summary>An instructor with a vehicle, and a student with an active contract of the same category.</summary>
    private async Task<Scene> SceneAsync()
    {
        var instructor = TrainingRecords.Person("Инструкторов");
        var student = TrainingRecords.Person("Водителев");
        var course = TrainingRecords.NewCourse();
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var vehicle = NewVehicle();
        await SaveAsync(instructor, student, course, contract, vehicle);

        return new Scene(instructor, student, contract, vehicle);
    }

    private static DrivingSlot Slot(Scene scene, TimeInterval time, Guid? instructorId = null, Vehicle? vehicle = null) =>
        DrivingSlot.Offer(instructorId ?? scene.Instructor.Id, vehicle ?? scene.Vehicle, null, null, time, Now).Value;

    private async Task ChangeAsync(Guid slotId, Func<DrivingSlot, Contract, Result> change)
    {
        await using var context = database.CreateContext();
        var slot = await new DrivingSlotRepository(context).GetByIdAsync(slotId, CancellationToken);
        var contractId = slot!.CurrentBooking?.ContractId;
        var contract = contractId is null ? null : await new ContractRepository(context).GetByIdAsync(contractId.Value, CancellationToken);
        change(slot, contract!).IsSuccess.Should().BeTrue();
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_CompletedLesson_StoresTheBookingWithItsSkillGrades()
    {
        var scene = await SceneAsync();
        var slot = Slot(scene, Hours(9, 11));
        slot.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(slot);
        SkillGrade[] grades =
        [
            SkillGrade.Create(DrivingSkill.VehicleControl, 4).Value,
            SkillGrade.Create(DrivingSkill.GearShifting, 5).Value
        ];

        await ChangeAsync(slot.Id, (tracked, contract) =>
            tracked.Complete(contract, 2m, grades, "Уверенно", 10, _lessonDay.AddHours(11)));

        await using var context = database.CreateContext();
        var loaded = await new DrivingSlotRepository(context).GetByIdAsync(slot.Id, CancellationToken);
        loaded!.Status.Should().Be(DrivingSlotStatus.Completed);
        var booking = loaded.Bookings.Single();
        booking.Status.Should().Be(DrivingBookingStatus.Completed);
        booking.ActualHours.Should().Be(2m);
        booking.SkillGrades.Should().Equal(grades);
        booking.InstructorNote.Should().Be("Уверенно");
        (await new ContractRepository(context).GetByIdAsync(scene.Contract.Id, CancellationToken))!
            .CompletedPracticeHours.Should().Be(2m);
    }

    [Fact]
    public async Task Overlaps_AreFoundForTheInstructorTheVehicleAndTheStudent()
    {
        var scene = await SceneAsync();
        var slot = Slot(scene, Hours(9, 11, day: 1));
        slot.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(slot);
        await using var context = database.CreateContext();
        var slots = new DrivingSlotRepository(context);

        (await slots.HasOverlapAsync(scene.Instructor.Id, Guid.CreateVersion7(), Hours(10, 12, day: 1), CancellationToken))
            .Should().BeTrue();
        (await slots.HasOverlapAsync(Guid.CreateVersion7(), scene.Vehicle.Id, Hours(10, 12, day: 1), CancellationToken))
            .Should().BeTrue();
        (await slots.HasOverlapAsync(scene.Instructor.Id, scene.Vehicle.Id, Hours(11, 12, day: 1), CancellationToken))
            .Should().BeFalse();
        (await slots.StudentHasBookingAtAsync(scene.Student.Id, Hours(10, 12, day: 1), Guid.CreateVersion7(), CancellationToken))
            .Should().BeTrue();
        (await slots.StudentHasBookingAtAsync(scene.Student.Id, Hours(10, 12, day: 1), slot.Id, CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Save_OverlappingSlotOfTheVehicle_IsRejectedByTheDatabase()
    {
        var scene = await SceneAsync();
        await SaveAsync(Slot(scene, Hours(9, 11, day: 2)));

        var save = () => SaveAsync(Slot(scene, Hours(10, 12, day: 2), instructorId: Guid.CreateVersion7()));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Save_OverlappingActiveBookingsOfTheStudent_AreRejectedByTheDatabase()
    {
        var scene = await SceneAsync();
        var otherInstructor = TrainingRecords.Person("Второй");
        var otherVehicle = NewVehicle();
        await SaveAsync(otherInstructor, otherVehicle);
        var first = Slot(scene, Hours(9, 11, day: 3));
        first.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(first);
        var second = Slot(scene, Hours(10, 12, day: 3), otherInstructor.Id, otherVehicle);
        second.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();

        var save = () => SaveAsync(second);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task BookedSlots_AreReadForTheContractAndForReminders()
    {
        var scene = await SceneAsync();
        var past = Slot(scene, Hours(8, 9, day: 4));
        var future = Slot(scene, Hours(14, 15, day: 4));
        var free = Slot(scene, Hours(16, 17, day: 4));
        past.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        future.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        await SaveAsync(past, future, free);
        await using var context = database.CreateContext();
        var slots = new DrivingSlotRepository(context);

        var ofContract = await slots.GetBookedForContractAfterAsync(
            scene.Contract.Id, _lessonDay.AddDays(4).AddHours(12), CancellationToken);
        var forReminders = await slots.GetBookedStartingBetweenAsync(
            _lessonDay.AddDays(4).AddHours(8), _lessonDay.AddDays(4).AddHours(16), CancellationToken);

        ofContract.Select(slot => slot.Id).Should().Equal(future.Id);
        ofContract[0].CurrentBooking!.ContractId.Should().Be(scene.Contract.Id);
        forReminders.Select(slot => slot.Id).Should().Equal(future.Id);
    }

    [Fact]
    public async Task Queries_ReadTheScheduleAvailableSlotsAndTheStudentsLessons()
    {
        var scene = await SceneAsync();
        var booked = Slot(scene, Hours(9, 10, day: 5));
        var cancelled = Slot(scene, Hours(11, 12, day: 5));
        var free = Slot(scene, Hours(13, 14, day: 5));
        booked.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        cancelled.Book(scene.Contract, Now).IsSuccess.Should().BeTrue();
        cancelled.CancelBooking(null, "Заболел", TimeSpan.FromHours(24), Now).IsSuccess.Should().BeTrue();
        await SaveAsync(booked, cancelled, free);
        await using var context = database.CreateContext();
        var queries = new DrivingSlotQueries(context);
        var day = TimeInterval.Create(_lessonDay.AddDays(5), _lessonDay.AddDays(6)).Value;

        var schedule = await queries.GetScheduleAsync(
            new DrivingScheduleFilter(day, scene.Instructor.Id, null), DataScope.SchoolWide, CancellationToken);
        var available = await queries.GetAvailableAsync(
            new AvailableDrivingSlotFilter(day, Now, LicenseCategory.B, Transmission.Manual, scene.Instructor.Id),
            DataScope.SchoolWide, CancellationToken);
        var ofStudent = await queries.GetOfStudentAsync(scene.Student.Id, day, CancellationToken);
        var details = await queries.GetDetailsAsync(cancelled.Id, CancellationToken);

        schedule.Select(item => item.Slot.Id).Should().Equal(booked.Id, cancelled.Id, free.Id);
        schedule[0].Booking!.ContractNumber.Should().Be(scene.Contract.Number);
        schedule[0].Booking!.StudentName.Should().Be("Водителев Иван Петрович");
        schedule[1].Booking.Should().BeNull();
        schedule[0].Slot.VehicleName.Should().Be("Lada Vesta");
        schedule[0].Slot.InstructorName.Should().Be("Инструкторов Иван Петрович");
        available.Select(item => item.Id).Should().Equal(cancelled.Id, free.Id);
        ofStudent.Select(item => (item.Slot.Id, item.Booking.Status))
            .Should().Equal((booked.Id, DrivingBookingStatus.Active), (cancelled.Id, DrivingBookingStatus.Cancelled));
        details!.Bookings.Should().ContainSingle().Which.CancellationReason.Should().Be("Заболел");
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    private sealed record Scene(User Instructor, User Student, Contract Contract, Vehicle Vehicle);
}

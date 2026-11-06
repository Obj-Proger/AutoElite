using System.Linq.Expressions;
using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingSlots;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads driving slots with their instructor, vehicle, ground, route and bookings, without tracking. A slot belongs to
/// the branch of its vehicle, and a slot with a shared vehicle is in every scope.
/// </summary>
internal sealed class DrivingSlotQueries(AutoEliteDbContext dbContext) : IDrivingSlotQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DrivingScheduleItem>> GetScheduleAsync(
        DrivingScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var rows = InScope(Overlapping(Rows(), filter.Period), scope);
        if (filter.InstructorId is { } instructorId)
            rows = rows.Where(row => row.InstructorId == instructorId);

        if (filter.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        var slots = await Earliest(rows).ToListAsync(cancellationToken);
        var bookings = await BookingsAsync(
            [.. slots.Select(slot => slot.Id)], booking => booking.Status != DrivingBookingStatus.Cancelled, cancellationToken);

        return
        [
            .. slots.Select(slot => new DrivingScheduleItem(ToItem(slot), bookings.GetValueOrDefault(slot.Id)?.Single()))
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DrivingSlotItem>> GetAvailableAsync(
        AvailableDrivingSlotFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var rows = InScope(Overlapping(Rows(), filter.Period), scope)
            .Where(row => row.Status == DrivingSlotStatus.Available
                && row.StartUtc > filter.StartsAfterUtc
                && row.Category == filter.Category
                && row.Transmission == filter.Transmission);
        if (filter.InstructorId is { } instructorId)
            rows = rows.Where(row => row.InstructorId == instructorId);

        var slots = await Earliest(rows).ToListAsync(cancellationToken);

        return [.. slots.Select(ToItem)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudentDrivingLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default)
    {
        var bookingsOfStudent = dbContext.Set<DrivingBooking>();
        var slots = await Earliest(Overlapping(Rows(), period)
                .Where(row => bookingsOfStudent.Any(booking =>
                    EF.Property<Guid>(booking, DrivingSlotConfiguration.SlotKey) == row.Id && booking.StudentId == studentId)))
            .ToListAsync(cancellationToken);
        var bookings = await BookingsAsync(
            [.. slots.Select(slot => slot.Id)], booking => booking.StudentId == studentId, cancellationToken);

        return
        [
            .. slots.SelectMany(slot =>
                bookings[slot.Id].Select(booking => new StudentDrivingLessonItem(ToItem(slot), booking)))
        ];
    }

    /// <inheritdoc />
    public async Task<DrivingSlotDetails?> GetDetailsAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        var slot = await Rows().FirstOrDefaultAsync(row => row.Id == slotId, cancellationToken);
        if (slot is null)
            return null;

        var bookings = await BookingsAsync([slotId], _ => true, cancellationToken);

        return new DrivingSlotDetails(ToItem(slot), bookings.GetValueOrDefault(slotId) ?? []);
    }

    private static IQueryable<SlotRow> Overlapping(IQueryable<SlotRow> rows, TimeInterval period) =>
        rows.Where(row => row.StartUtc < period.EndUtc && period.StartUtc < row.EndUtc);

    private static IQueryable<SlotRow> InScope(IQueryable<SlotRow> rows, DataScope scope) =>
        scope.IsSchoolWide ? rows : rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

    private static IQueryable<SlotRow> Earliest(IQueryable<SlotRow> rows) =>
        rows.OrderBy(row => row.StartUtc).ThenBy(row => row.Id);

    /// <summary>Reads the bookings of the slots that pass the condition, by slot, in the order they were made.</summary>
    private async Task<Dictionary<Guid, List<DrivingBookingItem>>> BookingsAsync(
        List<Guid> slotIds,
        Expression<Func<DrivingBooking, bool>> condition,
        CancellationToken cancellationToken)
    {
        if (slotIds.Count == 0)
            return new Dictionary<Guid, List<DrivingBookingItem>>();

        var rows = await (
                from booking in dbContext.Set<DrivingBooking>().AsNoTracking().Where(condition)
                join contract in dbContext.Set<Contract>() on booking.ContractId equals contract.Id
                join student in dbContext.Set<User>() on booking.StudentId equals student.Id
                where slotIds.Contains(EF.Property<Guid>(booking, DrivingSlotConfiguration.SlotKey))
                orderby booking.BookedAtUtc, booking.Id
                select new
                {
                    SlotId = EF.Property<Guid>(booking, DrivingSlotConfiguration.SlotKey),
                    Booking = booking,
                    contract.Number,
                    student.FullName.LastName,
                    student.FullName.FirstName,
                    student.FullName.MiddleName
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.SlotId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => ToItem(
                        row.Booking, row.Number, PersonNames.Format(row.LastName, row.FirstName, row.MiddleName)!))
                    .ToList());
    }

    private static DrivingBookingItem ToItem(DrivingBooking booking, string contractNumber, string studentName) =>
        new(
            booking.Id, booking.ContractId, contractNumber, booking.StudentId, studentName, booking.Status,
            booking.BookedAtUtc, booking.CancelledAtUtc, booking.CancelledByUserId, booking.CancellationReason,
            booking.ActualHours, [.. booking.SkillGrades.Select(grade => new SkillGradeItem(grade.Skill, grade.Grade))],
            booking.AverageGrade, booking.InstructorNote, booking.CompletedAtUtc, booking.StudentRating,
            booking.StudentComment);

    private static DrivingSlotItem ToItem(SlotRow row) =>
        new(
            row.Id, row.InstructorId,
            PersonNames.Format(row.InstructorLastName, row.InstructorFirstName, row.InstructorMiddleName)!,
            row.VehicleId, row.VehiclePlate.Value,
            row.VehicleModel is null ? row.VehicleMake : $"{row.VehicleMake} {row.VehicleModel}",
            row.Category, row.Transmission, row.BranchId, row.TrainingGroundId, row.TrainingGroundName,
            row.TrainingGroundCity is null ? null : $"{row.TrainingGroundCity}, {row.TrainingGroundStreet}",
            row.RouteId, row.RouteName, row.StartUtc, row.EndUtc, row.Status);

    private IQueryable<SlotRow> Rows() =>
        from slot in dbContext.Set<DrivingSlot>().AsNoTracking()
        join instructor in dbContext.Set<User>() on slot.InstructorId equals instructor.Id
        join vehicle in dbContext.Set<Vehicle>() on slot.VehicleId equals vehicle.Id
        join ground in dbContext.Set<TrainingGround>() on slot.TrainingGroundId equals (Guid?)ground.Id into grounds
        from ground in grounds.DefaultIfEmpty()
        join route in dbContext.Set<DrivingRoute>() on slot.RouteId equals (Guid?)route.Id into routes
        from route in routes.DefaultIfEmpty()
        select new SlotRow
        {
            Id = slot.Id,
            InstructorId = slot.InstructorId,
            InstructorLastName = instructor.FullName.LastName,
            InstructorFirstName = instructor.FullName.FirstName,
            InstructorMiddleName = instructor.FullName.MiddleName,
            VehicleId = slot.VehicleId,
            VehiclePlate = vehicle.Plate,
            VehicleMake = vehicle.Make,
            VehicleModel = vehicle.Model,
            Category = slot.Category,
            Transmission = slot.Transmission,
            BranchId = vehicle.BranchId,
            TrainingGroundId = slot.TrainingGroundId,
            TrainingGroundName = ground == null ? null : ground.Name,
            TrainingGroundCity = ground == null ? null : ground.Address.City,
            TrainingGroundStreet = ground == null ? null : ground.Address.Street,
            RouteId = slot.RouteId,
            RouteName = route == null ? null : route.Name,
            StartUtc = slot.Time.StartUtc,
            EndUtc = slot.Time.EndUtc,
            Status = slot.Status
        };

    /// <summary>The columns of a slot that the read models are built from.</summary>
    private sealed class SlotRow
    {
        public Guid Id { get; init; }

        public Guid InstructorId { get; init; }

        public string InstructorLastName { get; init; } = null!;

        public string InstructorFirstName { get; init; } = null!;

        public string? InstructorMiddleName { get; init; }

        public Guid VehicleId { get; init; }

        public LicensePlate VehiclePlate { get; init; } = null!;

        public string VehicleMake { get; init; } = null!;

        public string? VehicleModel { get; init; }

        public LicenseCategory Category { get; init; }

        public Transmission Transmission { get; init; }

        public Guid? BranchId { get; init; }

        public Guid? TrainingGroundId { get; init; }

        public string? TrainingGroundName { get; init; }

        public string? TrainingGroundCity { get; init; }

        public string? TrainingGroundStreet { get; init; }

        public Guid? RouteId { get; init; }

        public string? RouteName { get; init; }

        public DateTime StartUtc { get; init; }

        public DateTime EndUtc { get; init; }

        public DrivingSlotStatus Status { get; init; }
    }
}

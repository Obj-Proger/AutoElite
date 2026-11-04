namespace AutoElite.Application.DrivingSlots;

/// <summary>The conditions of a driving schedule; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Period">Only slots that overlap this time.</param>
/// <param name="InstructorId">Only slots of this instructor.</param>
/// <param name="Status">Only slots in this status.</param>
public sealed record DrivingScheduleFilter(TimeInterval Period, Guid? InstructorId, DrivingSlotStatus? Status);

/// <summary>The conditions of the slots a contract can book.</summary>
/// <param name="Period">Only slots that overlap this time.</param>
/// <param name="StartsAfterUtc">Only slots that start later than this UTC time, normally now.</param>
/// <param name="Category">The licence category of the contract.</param>
/// <param name="Transmission">The gearbox of the contract.</param>
/// <param name="InstructorId">Only slots of this instructor; <see langword="null"/> for all.</param>
public sealed record AvailableDrivingSlotFilter(
    TimeInterval Period,
    DateTime StartsAfterUtc,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? InstructorId);

/// <summary>A driving slot without its bookings.</summary>
/// <param name="Id">The identifier of the slot.</param>
/// <param name="InstructorId">The driving instructor.</param>
/// <param name="InstructorName">The full name of the instructor.</param>
/// <param name="VehicleId">The vehicle.</param>
/// <param name="VehiclePlate">The licence plate of the vehicle.</param>
/// <param name="VehicleName">The make and model of the vehicle.</param>
/// <param name="Category">The licence category of the vehicle.</param>
/// <param name="Transmission">The gearbox of the vehicle.</param>
/// <param name="BranchId">The branch of the vehicle, if any; a slot with a shared vehicle is visible in every branch.</param>
/// <param name="TrainingGroundId">The ground where the lesson starts, if any.</param>
/// <param name="TrainingGroundName">The name of the ground, if any.</param>
/// <param name="TrainingGroundAddress">The address of the ground, if any.</param>
/// <param name="RouteId">The route, if any.</param>
/// <param name="RouteName">The name of the route, if any.</param>
/// <param name="StartUtc">The start in UTC.</param>
/// <param name="EndUtc">The end in UTC.</param>
/// <param name="Status">The slot status.</param>
public sealed record DrivingSlotItem(
    Guid Id,
    Guid InstructorId,
    string InstructorName,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleName,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId,
    Guid? TrainingGroundId,
    string? TrainingGroundName,
    string? TrainingGroundAddress,
    Guid? RouteId,
    string? RouteName,
    DateTime StartUtc,
    DateTime EndUtc,
    DrivingSlotStatus Status);

/// <summary>A grade of one driving skill.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Grade">The grade, from 1 to 5.</param>
public sealed record SkillGradeItem(DrivingSkill Skill, int Grade);

/// <summary>A booking of a slot with the outcome of the lesson.</summary>
/// <param name="Id">The identifier of the booking.</param>
/// <param name="ContractId">The contract.</param>
/// <param name="ContractNumber">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="Status">The booking status.</param>
/// <param name="BookedAtUtc">When the slot was booked.</param>
/// <param name="CancelledAtUtc">When the booking was cancelled, if it was.</param>
/// <param name="CancelledByUserId">Who cancelled it; <see langword="null"/> for the system or if not cancelled.</param>
/// <param name="CancellationReason">Why it was cancelled, if it was.</param>
/// <param name="ActualHours">The hours of a completed lesson.</param>
/// <param name="SkillGrades">The skill grades of a completed lesson, empty if it was not graded.</param>
/// <param name="AverageGrade">The average of the skill grades, if any.</param>
/// <param name="InstructorNote">The instructor's note about the lesson, if any.</param>
/// <param name="CompletedAtUtc">When the lesson was completed or the no-show recorded.</param>
/// <param name="StudentRating">The student's rating of the lesson, if any.</param>
/// <param name="StudentComment">The student's comment, if any.</param>
public sealed record DrivingBookingItem(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    Guid StudentId,
    string StudentName,
    DrivingBookingStatus Status,
    DateTime BookedAtUtc,
    DateTime? CancelledAtUtc,
    Guid? CancelledByUserId,
    string? CancellationReason,
    decimal? ActualHours,
    IReadOnlyList<SkillGradeItem> SkillGrades,
    decimal? AverageGrade,
    string? InstructorNote,
    DateTime? CompletedAtUtc,
    int? StudentRating,
    string? StudentComment);

/// <summary>A slot in an instructor's schedule with the booking that holds it.</summary>
/// <param name="Slot">The slot.</param>
/// <param name="Booking">The booking that is not cancelled, or <see langword="null"/> for a free or withdrawn slot.</param>
public sealed record DrivingScheduleItem(DrivingSlotItem Slot, DrivingBookingItem? Booking);

/// <summary>A driving lesson of a student: one of their bookings, cancelled ones included.</summary>
/// <param name="Slot">The slot.</param>
/// <param name="Booking">The student's booking of the slot.</param>
public sealed record StudentDrivingLessonItem(DrivingSlotItem Slot, DrivingBookingItem Booking);

/// <summary>The card of a slot with all of its bookings.</summary>
/// <param name="Slot">The slot.</param>
/// <param name="Bookings">All bookings, cancelled ones included, in the order they were made.</param>
public sealed record DrivingSlotDetails(DrivingSlotItem Slot, IReadOnlyList<DrivingBookingItem> Bookings);

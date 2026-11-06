using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A period a driving instructor offers for a practical lesson, with a vehicle and optionally a training ground and a
/// route, and the bookings students made for it.
/// </summary>
/// <remarks>
/// <para>
/// At most one booking is active at a time, so the slot cannot be booked twice. A cancelled booking frees the slot for
/// another student and is kept as history.
/// </para>
/// <para>
/// Overlaps between slots of the same instructor or vehicle, and between bookings of the same student, involve other
/// slots and are prevented by the application layer and a database exclusion constraint.
/// </para>
/// <para>
/// Completing the lesson adds the actual hours and the average skill grade to the student's contract in the same call;
/// the caller saves the slot and the contract in one unit of work.
/// </para>
/// </remarks>
public sealed class DrivingSlot : AuditableEntity
{
    /// <summary>The longest slot that can be offered.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(4);

    /// <summary>The longest reason of a cancellation or withdrawal.</summary>
    public const int MaxReasonLength = 1000;

    /// <summary>The longest instructor note or student comment.</summary>
    public const int MaxNoteLength = 2000;
    private const int MinRating = 1;
    private const int MaxRating = 5;

    private readonly List<DrivingBooking> _bookings = [];

    /// <summary>Gets the identifier of the driving instructor.</summary>
    public Guid InstructorId { get; private set; }

    /// <summary>Gets the identifier of the vehicle.</summary>
    public Guid VehicleId { get; private set; }

    /// <summary>Gets the licence category of the vehicle, copied when the slot was offered.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the gearbox of the vehicle, copied when the slot was offered.</summary>
    public Transmission Transmission { get; private set; }

    /// <summary>Gets the identifier of the training ground, or <see langword="null"/> if the lesson starts elsewhere.</summary>
    public Guid? TrainingGroundId { get; private set; }

    /// <summary>Gets the identifier of the planned route, or <see langword="null"/> if none is planned.</summary>
    public Guid? RouteId { get; private set; }

    /// <summary>Gets when the lesson takes place.</summary>
    public TimeInterval Time { get; private set; } = null!;

    /// <summary>Gets the slot status.</summary>
    public DrivingSlotStatus Status { get; private set; }

    /// <summary>Gets all bookings of the slot, including cancelled ones, in the order they were made.</summary>
    public IReadOnlyList<DrivingBooking> Bookings => _bookings.AsReadOnly();

    /// <summary>
    /// Gets the booking that holds or held the lesson: the active one, or the completed or no-show one once the slot is
    /// completed; <see langword="null"/> otherwise.
    /// </summary>
    public DrivingBooking? CurrentBooking =>
        _bookings.Find(booking => booking.Status is not DrivingBookingStatus.Cancelled);

    private DrivingSlot(Guid id, Guid instructorId, Vehicle vehicle, Guid? trainingGroundId, Guid? routeId, TimeInterval time)
        : base(id)
    {
        InstructorId = instructorId;
        VehicleId = vehicle.Id;
        Category = vehicle.Category;
        Transmission = vehicle.Transmission;
        TrainingGroundId = trainingGroundId;
        RouteId = routeId;
        Time = time;
        Status = DrivingSlotStatus.Available;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private DrivingSlot()
    {
    }

    /// <summary>Offers a new slot for booking.</summary>
    /// <param name="instructorId">The driving instructor; checking the role is up to the caller.</param>
    /// <param name="vehicle">The vehicle; it must be active. Its category and gearbox are copied.</param>
    /// <param name="trainingGround">The training ground, if any; it must be active.</param>
    /// <param name="route">The planned route, if any; it must be active.</param>
    /// <param name="time">When the lesson takes place; it must start in the future and last at most <see cref="MaxDuration"/>.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The slot, or the first validation failure encountered.</returns>
    public static Result<DrivingSlot> Offer(
        Guid instructorId, Vehicle vehicle, TrainingGround? trainingGround, DrivingRoute? route, TimeInterval time, DateTime utcNow)
    {
        var instructorResult = Guard.NotEmpty(instructorId, DomainErrors.DrivingSlot.InstructorIdEmpty);
        if (instructorResult.IsFailure)
            return Result.Failure<DrivingSlot>(instructorResult.Error);

        if (!vehicle.IsActive)
            return Result.Failure<DrivingSlot>(DomainErrors.DrivingSlot.VehicleInactive);

        if (trainingGround is { IsActive: false })
            return Result.Failure<DrivingSlot>(DomainErrors.DrivingSlot.TrainingGroundInactive);

        if (route is { IsActive: false })
            return Result.Failure<DrivingSlot>(DomainErrors.DrivingSlot.RouteInactive);

        if (time.StartUtc <= utcNow)
            return Result.Failure<DrivingSlot>(DomainErrors.DrivingSlot.StartInPast);

        if (time.Duration > MaxDuration)
            return Result.Failure<DrivingSlot>(DomainErrors.DrivingSlot.DurationTooLong);

        return new DrivingSlot(Guid.CreateVersion7(), instructorId, vehicle, trainingGround?.Id, route?.Id, time);
    }

    /// <summary>Books the slot for the student of a contract and raises <see cref="DrivingLessonBookedDomainEvent"/>.</summary>
    /// <param name="contract">The student's contract; it must be active and for the vehicle's licence category and gearbox.</param>
    /// <param name="utcNow">The current UTC time; the lesson must not have started.</param>
    /// <returns>The booking, or a failure if the slot is not available or has started, or the contract does not fit.</returns>
    public Result<DrivingBooking> Book(Contract contract, DateTime utcNow)
    {
        if (Status != DrivingSlotStatus.Available)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.NotAvailable);

        if (utcNow >= Time.StartUtc)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.AlreadyStarted);

        if (contract.Status != ContractStatus.Active)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.ContractNotActive);

        if (contract.Category != Category)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.CategoryMismatch);

        if (contract.Transmission != Transmission)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.TransmissionMismatch);

        var booking = DrivingBooking.Create(contract.Id, contract.StudentId, utcNow);
        _bookings.Add(booking);
        Status = DrivingSlotStatus.Booked;
        RaiseDomainEvent(new DrivingLessonBookedDomainEvent(Id, booking.Id, InstructorId, contract.StudentId, Time.StartUtc, utcNow));

        return booking;
    }

    /// <summary>
    /// Cancels the active booking before the lesson starts, frees the slot and raises
    /// <see cref="DrivingBookingCancelledDomainEvent"/>.
    /// </summary>
    /// <param name="cancelledByUserId">The student, instructor or staff member cancelling; <see langword="null"/> for the system.</param>
    /// <param name="reason">Why the booking is cancelled. Required.</param>
    /// <param name="studentCancellationNotice">
    /// How long before the start the student may still cancel (a school setting); later only staff can cancel.
    /// </param>
    /// <param name="utcNow">The current UTC time; the lesson must not have started.</param>
    /// <returns>
    /// A failure if there is no active booking, the lesson has started, the student cancels too late, or an argument
    /// is invalid.
    /// </returns>
    public Result CancelBooking(Guid? cancelledByUserId, string? reason, TimeSpan studentCancellationNotice, DateTime utcNow)
    {
        if (Status != DrivingSlotStatus.Booked)
            return Result.Failure(DomainErrors.DrivingSlot.NotBooked);

        if (utcNow >= Time.StartUtc)
            return Result.Failure(DomainErrors.DrivingSlot.AlreadyStarted);

        var argumentsResult = ValidateCancellation(cancelledByUserId, reason);
        if (argumentsResult.IsFailure)
            return argumentsResult;

        if (cancelledByUserId == CurrentBooking!.StudentId && Time.StartUtc - utcNow < studentCancellationNotice)
            return Result.Failure(DomainErrors.DrivingSlot.TooLateToCancel);

        CancelActiveBooking(cancelledByUserId, argumentsResult.Value, utcNow);
        Status = DrivingSlotStatus.Available;

        return Result.Success();
    }

    /// <summary>
    /// Withdraws the slot, for example when the instructor is ill. An active booking is cancelled with the same reason,
    /// and <see cref="DrivingBookingCancelledDomainEvent"/> notifies the student.
    /// </summary>
    /// <param name="withdrawnByUserId">The instructor or staff member withdrawing the slot.</param>
    /// <param name="reason">Why the slot is withdrawn. Required.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the slot is completed or already withdrawn, or an argument is invalid.</returns>
    public Result Withdraw(Guid? withdrawnByUserId, string? reason, DateTime utcNow)
    {
        if (Status is not (DrivingSlotStatus.Available or DrivingSlotStatus.Booked))
            return Result.Failure(DomainErrors.DrivingSlot.NotOpen);

        var argumentsResult = ValidateCancellation(withdrawnByUserId, reason);
        if (argumentsResult.IsFailure)
            return argumentsResult;

        if (Status == DrivingSlotStatus.Booked)
            CancelActiveBooking(withdrawnByUserId, argumentsResult.Value, utcNow);

        Status = DrivingSlotStatus.Withdrawn;

        return Result.Success();
    }

    /// <summary>
    /// Completes the lesson: records the actual hours, the skill grades and a note, adds the hours and the average grade
    /// to the contract, and raises <see cref="DrivingLessonCompletedDomainEvent"/> so the student is invited to rate it.
    /// </summary>
    /// <param name="contract">The contract of the active booking.</param>
    /// <param name="actualHours">The actual length of the lesson; the contract checks the range.</param>
    /// <param name="skillGrades">The grades per skill, at most one per skill; may be empty.</param>
    /// <param name="instructorNote">An optional note for the student and the school.</param>
    /// <param name="qualityWindowSize">The school's quality window, passed to the contract.</param>
    /// <param name="utcNow">The current UTC time; the lesson must have started.</param>
    /// <returns>A failure if there is no active booking, the lesson has not started, or an argument or the contract rejects the change.</returns>
    public Result Complete(
        Contract contract,
        decimal actualHours,
        IReadOnlyCollection<SkillGrade> skillGrades,
        string? instructorNote,
        int qualityWindowSize,
        DateTime utcNow)
    {
        var bookingResult = GetActiveBookingForLessonOutcome(contract.Id, utcNow);
        if (bookingResult.IsFailure)
            return bookingResult;

        if (skillGrades.Select(grade => grade.Skill).Distinct().Count() != skillGrades.Count)
            return Result.Failure(DomainErrors.DrivingSlot.DuplicateSkillGrade);

        var noteResult = Guard.OptionalText(instructorNote, MaxNoteLength, DomainErrors.DrivingSlot.NoteTooLong);
        if (noteResult.IsFailure)
            return Result.Failure(noteResult.Error);

        var contractResult = contract.RecordPracticeLesson(actualHours, DrivingBooking.AverageOf(skillGrades), qualityWindowSize);
        if (contractResult.IsFailure)
            return contractResult;

        var booking = bookingResult.Value;
        booking.Complete(actualHours, skillGrades, noteResult.Value, utcNow);
        Status = DrivingSlotStatus.Completed;
        RaiseDomainEvent(new DrivingLessonCompletedDomainEvent(Id, booking.Id, InstructorId, booking.StudentId, utcNow));

        return Result.Success();
    }

    /// <summary>Records that the student did not show up. Nothing is added to the contract.</summary>
    /// <param name="contract">The contract of the active booking.</param>
    /// <param name="instructorNote">An optional note.</param>
    /// <param name="utcNow">The current UTC time; the lesson must have started.</param>
    /// <returns>A failure if there is no active booking, the lesson has not started, or the note is too long.</returns>
    public Result MarkNoShow(Contract contract, string? instructorNote, DateTime utcNow)
    {
        var bookingResult = GetActiveBookingForLessonOutcome(contract.Id, utcNow);
        if (bookingResult.IsFailure)
            return bookingResult;

        var noteResult = Guard.OptionalText(instructorNote, MaxNoteLength, DomainErrors.DrivingSlot.NoteTooLong);
        if (noteResult.IsFailure)
            return Result.Failure(noteResult.Error);

        bookingResult.Value.MarkNoShow(noteResult.Value, utcNow);
        Status = DrivingSlotStatus.Completed;

        return Result.Success();
    }

    /// <summary>
    /// Records the student's rating of a completed lesson. A rating at or below <paramref name="lowRatingThreshold"/>
    /// raises <see cref="LowDrivingLessonRatingDomainEvent"/> so the responsible managers are notified.
    /// </summary>
    /// <param name="rating">The rating from 1 to 5.</param>
    /// <param name="comment">An optional comment.</param>
    /// <param name="lowRatingThreshold">The school's threshold for a low rating.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the lesson was not completed, it was already rated, or an argument is invalid.</returns>
    public Result Rate(int rating, string? comment, int lowRatingThreshold, DateTime utcNow)
    {
        var booking = CurrentBooking;
        if (booking is not { Status: DrivingBookingStatus.Completed })
            return Result.Failure(DomainErrors.DrivingSlot.LessonNotCompleted);

        if (booking.StudentRating is not null)
            return Result.Failure(DomainErrors.DrivingSlot.AlreadyRated);

        if (rating is < MinRating or > MaxRating)
            return Result.Failure(DomainErrors.DrivingSlot.RatingOutOfRange);

        var commentResult = Guard.OptionalText(comment, MaxNoteLength, DomainErrors.DrivingSlot.RatingCommentTooLong);
        if (commentResult.IsFailure)
            return Result.Failure(commentResult.Error);

        booking.Rate(rating, commentResult.Value, utcNow);

        if (rating <= lowRatingThreshold)
            RaiseDomainEvent(new LowDrivingLessonRatingDomainEvent(Id, booking.Id, InstructorId, booking.StudentId, rating, utcNow));

        return Result.Success();
    }

    private Result<DrivingBooking> GetActiveBookingForLessonOutcome(Guid contractId, DateTime utcNow)
    {
        if (Status != DrivingSlotStatus.Booked)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.NotBooked);

        if (utcNow < Time.StartUtc)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.NotStarted);

        var booking = CurrentBooking!;
        if (booking.ContractId != contractId)
            return Result.Failure<DrivingBooking>(DomainErrors.DrivingSlot.ContractMismatch);

        return booking;
    }

    private void CancelActiveBooking(Guid? cancelledByUserId, string reason, DateTime utcNow)
    {
        var booking = CurrentBooking!;
        booking.Cancel(cancelledByUserId, reason, utcNow);
        RaiseDomainEvent(new DrivingBookingCancelledDomainEvent(
            Id, booking.Id, InstructorId, booking.StudentId, cancelledByUserId, reason, Time.StartUtc, utcNow));
    }

    private static Result<string> ValidateCancellation(Guid? userId, string? reason)
    {
        if (userId is { } id && id == Guid.Empty)
            return Result.Failure<string>(DomainErrors.DrivingSlot.UserIdEmpty);

        return Guard.RequiredText(reason, MaxReasonLength, DomainErrors.DrivingSlot.ReasonEmpty, DomainErrors.DrivingSlot.ReasonTooLong);
    }
}

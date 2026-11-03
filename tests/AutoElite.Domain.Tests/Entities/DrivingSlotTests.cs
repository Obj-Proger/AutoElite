namespace AutoElite.Domain.Tests.Entities;

public class DrivingSlotTests
{
    private static readonly DateTime _now = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _lessonStart = new(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _afterLesson = _lessonStart.AddHours(2);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _instructorId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _studentUserId = new("66666666-6666-6666-6666-666666666666");
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Vehicle _vehicle = Vehicle.Register(
        LicensePlate.Create("А123ВС96").Value, "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, _now).Value;
    private static readonly Course _course =
        Course.Create("Category B", LicenseCategory.B, Money.Create(45000m, _rub).Value, 134, 56, null).Value;

    private static readonly SkillGrade[] _grades =
    [
        SkillGrade.Create(DrivingSkill.VehicleControl, 4).Value,
        SkillGrade.Create(DrivingSkill.Manoeuvring, 3).Value,
        SkillGrade.Create(DrivingSkill.TrafficRules, 5).Value
    ];

    private static TimeInterval Interval(DateTime start, double hours = 1.5) =>
        TimeInterval.Create(start, start.AddHours(hours)).Value;

    private static Contract CreateContract(Course? course = null)
    {
        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;

        return Contract.Create("AE-1", student, course ?? _course, Money.Create(45000m, _rub).Value, null, _start, _end, _now).Value;
    }

    private static DrivingSlot OfferSlot() =>
        DrivingSlot.Offer(_instructorId, _vehicle, null, null, Interval(_lessonStart), _now).Value;

    private static DrivingSlot BookedSlot(Contract contract)
    {
        var slot = OfferSlot();
        slot.Book(contract, _now);

        return slot;
    }

    private static DrivingSlot CompletedSlot(Contract contract)
    {
        var slot = BookedSlot(contract);
        slot.Complete(contract, 1.5m, _grades, null, _afterLesson);

        return slot;
    }

    [Fact]
    public void Offer_ValidData_ReturnsAvailableSlotWithGivenValues()
    {
        var ground = TrainingGround.Create("North ground", Address.Create("Yekaterinburg", "Shefskaya 2").Value, null, null).Value;
        var route = DrivingRoute.Create("Exam route", "Lenina - Malysheva", _instructorId, null).Value;

        var result = DrivingSlot.Offer(_instructorId, _vehicle, ground, route, Interval(_lessonStart), _now);

        result.IsSuccess.Should().BeTrue();
        var slot = result.Value;
        slot.InstructorId.Should().Be(_instructorId);
        slot.VehicleId.Should().Be(_vehicle.Id);
        slot.Category.Should().Be(LicenseCategory.B);
        slot.Transmission.Should().Be(Transmission.Manual);
        slot.TrainingGroundId.Should().Be(ground.Id);
        slot.RouteId.Should().Be(route.Id);
        slot.Time.Should().Be(Interval(_lessonStart));
        slot.Status.Should().Be(DrivingSlotStatus.Available);
        slot.CurrentBooking.Should().BeNull();
    }

    [Fact]
    public void Offer_EmptyInstructorId_ReturnsInstructorIdEmptyError()
    {
        var result = DrivingSlot.Offer(Guid.Empty, _vehicle, null, null, Interval(_lessonStart), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.InstructorIdEmpty);
    }

    [Fact]
    public void Offer_InactiveVehicle_ReturnsVehicleInactiveError()
    {
        var vehicle = Vehicle.Register(LicensePlate.Create("В456ОР96").Value, "Kia", null, 2020, LicenseCategory.B, Transmission.Manual, null, _now).Value;
        vehicle.Deactivate();

        var result = DrivingSlot.Offer(_instructorId, vehicle, null, null, Interval(_lessonStart), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.VehicleInactive);
    }

    [Fact]
    public void Offer_InactiveGround_ReturnsTrainingGroundInactiveError()
    {
        var ground = TrainingGround.Create("Old ground", Address.Create("Yekaterinburg", "Shefskaya 2").Value, null, null).Value;
        ground.Deactivate();

        var result = DrivingSlot.Offer(_instructorId, _vehicle, ground, null, Interval(_lessonStart), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.TrainingGroundInactive);
    }

    [Fact]
    public void Offer_InactiveRoute_ReturnsRouteInactiveError()
    {
        var route = DrivingRoute.Create("Old route", "Lenina", _instructorId, null).Value;
        route.Deactivate();

        var result = DrivingSlot.Offer(_instructorId, _vehicle, null, route, Interval(_lessonStart), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.RouteInactive);
    }

    [Fact]
    public void Offer_StartAtCurrentTime_ReturnsStartInPastError()
    {
        var result = DrivingSlot.Offer(_instructorId, _vehicle, null, null, Interval(_now), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.StartInPast);
    }

    [Theory]
    [InlineData(4.0, true)]
    [InlineData(4.25, false)]
    public void Offer_Duration_IsLimitedToFourHours(double hours, bool expectedSuccess)
    {
        var result = DrivingSlot.Offer(_instructorId, _vehicle, null, null, Interval(_lessonStart, hours), _now);

        result.IsSuccess.Should().Be(expectedSuccess);
    }

    [Fact]
    public void Book_AvailableSlot_CreatesActiveBookingAndRaisesBookedEvent()
    {
        var slot = OfferSlot();
        var contract = CreateContract();

        var result = slot.Book(contract, _now);

        result.IsSuccess.Should().BeTrue();
        var booking = result.Value;
        booking.ContractId.Should().Be(contract.Id);
        booking.StudentId.Should().Be(contract.StudentId);
        booking.BookedAtUtc.Should().Be(_now);
        booking.Status.Should().Be(DrivingBookingStatus.Active);
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
        slot.CurrentBooking.Should().BeSameAs(booking);
        slot.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new DrivingLessonBookedDomainEvent(slot.Id, booking.Id, _instructorId, contract.StudentId, _lessonStart, _now));
    }

    [Fact]
    public void Book_BookedSlot_ReturnsNotAvailableError()
    {
        var slot = BookedSlot(CreateContract());

        var result = slot.Book(CreateContract(), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotAvailable);
        slot.Bookings.Should().ContainSingle();
    }

    [Fact]
    public void Book_AtLessonStart_ReturnsAlreadyStartedError()
    {
        var slot = OfferSlot();

        var result = slot.Book(CreateContract(), _lessonStart);

        result.Error.Should().Be(DomainErrors.DrivingSlot.AlreadyStarted);
    }

    [Fact]
    public void Book_TerminatedContract_ReturnsContractNotActiveError()
    {
        var contract = CreateContract();
        contract.Terminate("Moved away", _start, _now);

        var result = OfferSlot().Book(contract, _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.ContractNotActive);
    }

    [Fact]
    public void Book_ContractForAnotherCategory_ReturnsCategoryMismatchError()
    {
        var motorcycleCourse = Course.Create("Category A", LicenseCategory.A, Money.Create(30000m, _rub).Value, 130, 18, null).Value;

        var result = OfferSlot().Book(CreateContract(motorcycleCourse), _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.CategoryMismatch);
    }

    [Fact]
    public void CancelBooking_BeforeStart_CancelsBookingFreesSlotAndRaisesCancelledEvent()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);
        var booking = slot.CurrentBooking!;
        slot.ClearDomainEvents();

        var result = slot.CancelBooking(_studentUserId, "  Feeling unwell  ", _now);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Available);
        slot.CurrentBooking.Should().BeNull();
        booking.Status.Should().Be(DrivingBookingStatus.Cancelled);
        booking.CancelledByUserId.Should().Be(_studentUserId);
        booking.CancellationReason.Should().Be("Feeling unwell");
        booking.CancelledAtUtc.Should().Be(_now);
        slot.DomainEvents.Should().ContainSingle().Which.Should().Be(new DrivingBookingCancelledDomainEvent(
            slot.Id, booking.Id, _instructorId, contract.StudentId, _studentUserId, "Feeling unwell", _lessonStart, _now));
    }

    [Fact]
    public void CancelBooking_ThenBookAgain_KeepsCancelledBookingAsHistory()
    {
        var slot = BookedSlot(CreateContract());
        slot.CancelBooking(_studentUserId, "Feeling unwell", _now);

        var result = slot.Book(CreateContract(), _now);

        result.IsSuccess.Should().BeTrue();
        slot.Bookings.Should().HaveCount(2);
        slot.CurrentBooking.Should().BeSameAs(result.Value);
    }

    [Fact]
    public void CancelBooking_AvailableSlot_ReturnsNotBookedError()
    {
        var result = OfferSlot().CancelBooking(_studentUserId, "Feeling unwell", _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotBooked);
    }

    [Fact]
    public void CancelBooking_AfterStart_ReturnsAlreadyStartedError()
    {
        var slot = BookedSlot(CreateContract());

        var result = slot.CancelBooking(_studentUserId, "Feeling unwell", _lessonStart);

        result.Error.Should().Be(DomainErrors.DrivingSlot.AlreadyStarted);
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CancelBooking_EmptyReason_ReturnsReasonEmptyError(string? reason)
    {
        var slot = BookedSlot(CreateContract());

        var result = slot.CancelBooking(_studentUserId, reason, _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.ReasonEmpty);
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
    }

    [Fact]
    public void CancelBooking_EmptyUserId_ReturnsUserIdEmptyError()
    {
        var slot = BookedSlot(CreateContract());

        var result = slot.CancelBooking(Guid.Empty, "Feeling unwell", _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.UserIdEmpty);
    }

    [Fact]
    public void Withdraw_AvailableSlot_WithdrawsWithoutEvent()
    {
        var slot = OfferSlot();

        var result = slot.Withdraw(_instructorId, "Instructor is ill", _now);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Withdrawn);
        slot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Withdraw_BookedSlot_CancelsBookingAndNotifiesStudent()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);
        var booking = slot.CurrentBooking!;
        slot.ClearDomainEvents();

        var result = slot.Withdraw(_instructorId, "Instructor is ill", _now);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Withdrawn);
        booking.Status.Should().Be(DrivingBookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Instructor is ill");
        slot.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<DrivingBookingCancelledDomainEvent>();
    }

    [Fact]
    public void Withdraw_WithdrawnSlot_ReturnsNotOpenError()
    {
        var slot = OfferSlot();
        slot.Withdraw(_instructorId, "Instructor is ill", _now);

        var result = slot.Withdraw(_instructorId, "Again", _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotOpen);
    }

    [Fact]
    public void Withdraw_CompletedSlot_ReturnsNotOpenError()
    {
        var contract = CreateContract();
        var slot = CompletedSlot(contract);

        var result = slot.Withdraw(_instructorId, "Too late", _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotOpen);
    }

    [Fact]
    public void Complete_BookedSlotAfterStart_RecordsOutcomeAndUpdatesContract()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);
        slot.ClearDomainEvents();

        var result = slot.Complete(contract, 1.5m, _grades, "  Work on parking  ", _afterLesson);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Completed);
        var booking = slot.CurrentBooking!;
        booking.Status.Should().Be(DrivingBookingStatus.Completed);
        booking.ActualHours.Should().Be(1.5m);
        booking.SkillGrades.Should().Equal(_grades);
        booking.AverageGrade.Should().Be(4m);
        booking.InstructorNote.Should().Be("Work on parking");
        booking.CompletedAtUtc.Should().Be(_afterLesson);
        contract.CompletedPracticeHours.Should().Be(1.5m);
        contract.QualityScore.Should().Be(4m);
        slot.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new DrivingLessonCompletedDomainEvent(slot.Id, booking.Id, _instructorId, contract.StudentId, _afterLesson));
    }

    [Fact]
    public void Complete_WithoutGrades_AddsOnlyHoursToContract()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        slot.Complete(contract, 2m, [], null, _afterLesson);

        slot.CurrentBooking!.AverageGrade.Should().BeNull();
        contract.CompletedPracticeHours.Should().Be(2m);
        contract.QualityScore.Should().BeNull();
    }

    [Fact]
    public void Complete_BeforeStart_ReturnsNotStartedError()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.Complete(contract, 1.5m, _grades, null, _lessonStart.AddTicks(-1));

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotStarted);
    }

    [Fact]
    public void Complete_AvailableSlot_ReturnsNotBookedError()
    {
        var result = OfferSlot().Complete(CreateContract(), 1.5m, _grades, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotBooked);
    }

    [Fact]
    public void Complete_OtherContract_ReturnsContractMismatchError()
    {
        var contract = CreateContract();
        var otherContract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.Complete(otherContract, 1.5m, _grades, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.ContractMismatch);
        otherContract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public void Complete_SameSkillTwice_ReturnsDuplicateSkillGradeError()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);
        SkillGrade[] grades = [SkillGrade.Create(DrivingSkill.Manoeuvring, 3).Value, SkillGrade.Create(DrivingSkill.Manoeuvring, 4).Value];

        var result = slot.Complete(contract, 1.5m, grades, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.DuplicateSkillGrade);
    }

    [Fact]
    public void Complete_HoursRejectedByContract_ReturnsContractErrorWithoutChangingSlot()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.Complete(contract, 0m, _grades, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.Contract.LessonHoursOutOfRange);
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
        slot.CurrentBooking!.Status.Should().Be(DrivingBookingStatus.Active);
        slot.CurrentBooking!.SkillGrades.Should().BeEmpty();
    }

    [Fact]
    public void Complete_NoteTooLong_ReturnsNoteTooLongError()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.Complete(contract, 1.5m, _grades, new string('a', 2001), _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NoteTooLong);
        contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public void MarkNoShow_BookedSlotAfterStart_CompletesSlotWithoutTouchingContract()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.MarkNoShow(contract, "Did not answer the phone", _afterLesson);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Completed);
        slot.CurrentBooking!.Status.Should().Be(DrivingBookingStatus.NoShow);
        slot.CurrentBooking!.InstructorNote.Should().Be("Did not answer the phone");
        contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public void MarkNoShow_BeforeStart_ReturnsNotStartedError()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);

        var result = slot.MarkNoShow(contract, null, _now);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotStarted);
    }

    [Fact]
    public void Rate_CompletedLesson_StoresRating()
    {
        var slot = CompletedSlot(CreateContract());
        slot.ClearDomainEvents();

        var result = slot.Rate(5, "  Great lesson  ", _afterLesson);

        result.IsSuccess.Should().BeTrue();
        var booking = slot.CurrentBooking!;
        booking.StudentRating.Should().Be(5);
        booking.StudentComment.Should().Be("Great lesson");
        booking.RatedAtUtc.Should().Be(_afterLesson);
        slot.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Rate_LowRating_RaisesLowRatingEvent(int rating)
    {
        var contract = CreateContract();
        var slot = CompletedSlot(contract);
        slot.ClearDomainEvents();

        slot.Rate(rating, null, _afterLesson);

        slot.DomainEvents.Should().ContainSingle().Which.Should().Be(new LowDrivingLessonRatingDomainEvent(
            slot.Id, slot.CurrentBooking!.Id, _instructorId, contract.StudentId, rating, _afterLesson));
    }

    [Fact]
    public void Rate_RatingAboveThreshold_RaisesNoEvent()
    {
        var slot = CompletedSlot(CreateContract());
        slot.ClearDomainEvents();

        slot.Rate(3, null, _afterLesson);

        slot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rate_LessonNotCompleted_ReturnsLessonNotCompletedError()
    {
        var result = BookedSlot(CreateContract()).Rate(5, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.LessonNotCompleted);
    }

    [Fact]
    public void Rate_NoShow_ReturnsLessonNotCompletedError()
    {
        var contract = CreateContract();
        var slot = BookedSlot(contract);
        slot.MarkNoShow(contract, null, _afterLesson);

        var result = slot.Rate(5, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.LessonNotCompleted);
    }

    [Fact]
    public void Rate_AlreadyRated_ReturnsAlreadyRatedError()
    {
        var slot = CompletedSlot(CreateContract());
        slot.Rate(5, null, _afterLesson);

        var result = slot.Rate(1, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.AlreadyRated);
        slot.CurrentBooking!.StudentRating.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rate_RatingOutOfRange_ReturnsRatingOutOfRangeError(int rating)
    {
        var result = CompletedSlot(CreateContract()).Rate(rating, null, _afterLesson);

        result.Error.Should().Be(DomainErrors.DrivingSlot.RatingOutOfRange);
    }
}

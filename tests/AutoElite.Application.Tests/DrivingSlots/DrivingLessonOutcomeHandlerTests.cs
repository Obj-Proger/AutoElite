using AutoElite.Application.DrivingSlots;
using AutoElite.Application.DrivingSlots.CompleteDrivingLesson;
using AutoElite.Application.DrivingSlots.ContractClosure;
using AutoElite.Application.DrivingSlots.MarkDrivingNoShow;
using AutoElite.Application.DrivingSlots.RateDrivingLesson;
using AutoElite.Application.Settings;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.DrivingSlots;

public class DrivingLessonOutcomeHandlerTests
{
    private static readonly DateTime _start = DrivingSlotFixture.SlotStart;
    private static readonly FixedTimeProvider _duringTheLesson = new(new DateTimeOffset(_start.AddHours(1)));

    private readonly DrivingSlotFixture _fixture = new();

    private Task<Result> Complete(DrivingSlot slot, FakeCurrentUser? caller = null, IReadOnlyList<SkillGradeInput>? grades = null) =>
        new CompleteDrivingLessonCommandHandler(
                new DrivingSlotAccess(_fixture.Slots, caller ?? _fixture.Instructor), _fixture.Contracts,
                new SchoolSettingsProvider(_fixture.Settings), _duringTheLesson)
            .Handle(new CompleteDrivingLessonCommand(slot.Id, 1.5m, grades ?? [], "Good start"), TestContext.Current.CancellationToken);

    private Task<Result> Rate(DrivingSlot slot, FakeCurrentUser caller, int rating = 5) =>
        new RateDrivingLessonCommandHandler(_fixture.Slots, new SchoolSettingsProvider(_fixture.Settings), caller, _duringTheLesson)
            .Handle(new RateDrivingLessonCommand(slot.Id, rating, null), TestContext.Current.CancellationToken);

    private async Task<DrivingSlot> SeedCompletedSlot()
    {
        var slot = _fixture.SeedBookedSlot();
        await Complete(slot, grades: [new SkillGradeInput(DrivingSkill.VehicleControl, 4)]);

        return slot;
    }

    [Fact]
    public async Task Complete_StartedLesson_RecordsTheOutcomeOnTheSlotAndTheContract()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Complete(
            slot, grades: [new SkillGradeInput(DrivingSkill.VehicleControl, 4), new SkillGradeInput(DrivingSkill.TrafficRules, 5)]);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Completed);
        var booking = slot.CurrentBooking!;
        booking.ActualHours.Should().Be(1.5m);
        booking.AverageGrade.Should().Be(4.5m);
        booking.InstructorNote.Should().Be("Good start");
        _fixture.Contract.CompletedPracticeHours.Should().Be(1.5m);
    }

    [Fact]
    public async Task Complete_WithoutGrades_RecordsTheHoursOnly()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Complete(slot);

        result.IsSuccess.Should().BeTrue();
        slot.CurrentBooking!.AverageGrade.Should().BeNull();
        _fixture.Contract.CompletedPracticeHours.Should().Be(1.5m);
    }

    [Fact]
    public async Task Complete_SkillGradedTwice_ReturnsDuplicateSkillGradeAndKeepsTheContract()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Complete(
            slot, grades: [new SkillGradeInput(DrivingSkill.VehicleControl, 4), new SkillGradeInput(DrivingSkill.VehicleControl, 5)]);

        result.Error.Should().Be(DomainErrors.DrivingSlot.DuplicateSkillGrade);
        _fixture.Contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public async Task Complete_SlotWithoutABooking_ReturnsNotBooked()
    {
        var slot = _fixture.SeedSlot();

        var result = await Complete(slot);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotBooked);
    }

    [Fact]
    public async Task Complete_SlotOfAnotherInstructor_ReturnsDrivingSlotNotFound()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Complete(slot, FakeCurrentUser.SignedIn(UserRole.DrivingInstructor));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
    }

    [Fact]
    public void CompleteValidator_GradeOutOfRange_ReportsIt()
    {
        var command = new CompleteDrivingLessonCommand(Guid.CreateVersion7(), 1m, [new SkillGradeInput(DrivingSkill.Observation, 6)]);

        var result = new CompleteDrivingLessonCommandValidator().Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(DomainErrors.SkillGrade.GradeOutOfRange.Code);
    }

    [Fact]
    public async Task MarkNoShow_StartedLesson_CompletesTheSlotWithoutHours()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await new MarkDrivingNoShowCommandHandler(
                new DrivingSlotAccess(_fixture.Slots, _fixture.Instructor), _fixture.Contracts, _duringTheLesson)
            .Handle(new MarkDrivingNoShowCommand(slot.Id, "Did not answer the phone"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Completed);
        slot.CurrentBooking!.Status.Should().Be(DrivingBookingStatus.NoShow);
        _fixture.Contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public async Task Rate_OwnCompletedLesson_RecordsTheRating()
    {
        var slot = await SeedCompletedSlot();

        var result = await Rate(slot, _fixture.Student);

        result.IsSuccess.Should().BeTrue();
        slot.CurrentBooking!.StudentRating.Should().Be(5);
        slot.DomainEvents.Should().NotContain(domainEvent => domainEvent is LowDrivingLessonRatingDomainEvent);
    }

    [Fact]
    public async Task Rate_AtTheDefaultThreshold_RaisesLowRating()
    {
        var slot = await SeedCompletedSlot();

        await Rate(slot, _fixture.Student, rating: SchoolSettings.DefaultLowRatingThreshold);

        slot.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is LowDrivingLessonRatingDomainEvent);
    }

    [Fact]
    public async Task Rate_LessonOfAnotherStudent_ReturnsDrivingSlotNotFound()
    {
        var slot = await SeedCompletedSlot();

        var result = await Rate(slot, FakeCurrentUser.SignedIn(UserRole.Student));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
        slot.CurrentBooking!.StudentRating.Should().BeNull();
    }

    [Fact]
    public async Task ContractClosed_CancelsOnlyTheBookingsOfLessonsNotStarted()
    {
        var started = _fixture.SeedBookedSlot(TestData.Now.AddHours(1));
        var future = _fixture.SeedBookedSlot(_start);
        var closedAt = TestData.Now.AddHours(2);

        await new CancelBookingsWhenContractClosedHandler(_fixture.Slots).Handle(
            new ContractClosedDomainEvent(_fixture.Contract.Id, _fixture.Contract.StudentId, ContractStatus.Terminated, closedAt),
            TestContext.Current.CancellationToken);

        started.Status.Should().Be(DrivingSlotStatus.Booked);
        future.Status.Should().Be(DrivingSlotStatus.Available);
        var cancelled = future.Bookings.Should().ContainSingle().Which;
        cancelled.CancelledByUserId.Should().BeNull();
        cancelled.CancellationReason.Should().Be(CancelBookingsWhenContractClosedHandler.Reason);
        cancelled.CancelledAtUtc.Should().Be(closedAt);
    }
}

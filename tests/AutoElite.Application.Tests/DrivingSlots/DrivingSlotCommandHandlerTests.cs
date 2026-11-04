using AutoElite.Application.DrivingSlots;
using AutoElite.Application.DrivingSlots.BookDrivingSlot;
using AutoElite.Application.DrivingSlots.CancelDrivingBooking;
using AutoElite.Application.DrivingSlots.OfferDrivingSlot;
using AutoElite.Application.DrivingSlots.WithdrawDrivingSlot;
using AutoElite.Application.Settings;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.DrivingSlots;

public class DrivingSlotCommandHandlerTests
{
    private static readonly DateTime _start = DrivingSlotFixture.SlotStart;

    private readonly DrivingSlotFixture _fixture = new();

    private static FakeCurrentUser DrivingInstructor(Guid? branchId) => new()
    {
        UserId = Guid.CreateVersion7(),
        BranchId = branchId,
        Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
    };

    private Task<Result<Guid>> Offer(
        DateTime start, FakeCurrentUser? caller = null, Guid? vehicleId = null, Guid? groundId = null, Guid? routeId = null) =>
        new OfferDrivingSlotCommandHandler(
                _fixture.Slots, _fixture.Vehicles, _fixture.Grounds, _fixture.Routes, caller ?? _fixture.Instructor, TestData.Clock())
            .Handle(
                new OfferDrivingSlotCommand(vehicleId ?? _fixture.Vehicle.Id, start, start.AddHours(2), groundId, routeId),
                TestContext.Current.CancellationToken);

    private Task<Result> Book(DrivingSlot slot, FakeCurrentUser? caller = null, Guid? contractId = null) =>
        new BookDrivingSlotCommandHandler(_fixture.Slots, _fixture.Contracts, caller ?? _fixture.Student, TestData.Clock())
            .Handle(new BookDrivingSlotCommand(slot.Id, contractId ?? _fixture.Contract.Id), TestContext.Current.CancellationToken);

    private Task<Result> Cancel(DrivingSlot slot, FakeCurrentUser caller, DateTime? now = null) =>
        new CancelDrivingBookingCommandHandler(
                _fixture.Slots, _fixture.Contracts, new SchoolSettingsProvider(_fixture.Settings), caller,
                new FixedTimeProvider(new DateTimeOffset(now ?? TestData.Now)))
            .Handle(new CancelDrivingBookingCommand(slot.Id, "Feeling unwell"), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Offer_FreeTime_OffersTheSlotWithTheVehicleCategoryAndGearbox()
    {
        var result = await Offer(_start, groundId: _fixture.Ground.Id, routeId: _fixture.Route.Id);

        var slot = _fixture.Slots.Items.Should().ContainSingle().Which;
        slot.Id.Should().Be(result.Value);
        slot.InstructorId.Should().Be(_fixture.Instructor.UserId!.Value);
        slot.Category.Should().Be(LicenseCategory.B);
        slot.Transmission.Should().Be(Transmission.Manual);
        slot.TrainingGroundId.Should().Be(_fixture.Ground.Id);
        slot.RouteId.Should().Be(_fixture.Route.Id);
        slot.Status.Should().Be(DrivingSlotStatus.Available);
    }

    [Fact]
    public async Task Offer_OverlapsOwnSlot_ReturnsTimeConflict()
    {
        _fixture.SeedSlot(_start.AddHours(1), DrivingSlotFixture.CreateVehicle("B456CD96", null));

        var result = await Offer(_start);

        result.Error.Should().Be(ApplicationErrors.DrivingSlot.TimeConflict);
    }

    [Fact]
    public async Task Offer_VehicleBusyWithAnotherInstructor_ReturnsTimeConflict()
    {
        _fixture.SeedSlot(_start.AddHours(1), instructorId: Guid.CreateVersion7());

        var result = await Offer(_start);

        result.Error.Should().Be(ApplicationErrors.DrivingSlot.TimeConflict);
    }

    [Fact]
    public async Task Offer_OverlapsAWithdrawnSlot_Succeeds()
    {
        _fixture.SeedSlot(_start).Withdraw(_fixture.Instructor.UserId, "Car in repair", TestData.Now);

        var result = await Offer(_start);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Offer_RightAfterAnotherSlot_Succeeds()
    {
        _fixture.SeedSlot(_start.AddHours(-2));

        var result = await Offer(_start);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Offer_VehicleOfAnotherBranch_ReturnsVehicleNotFound()
    {
        var vehicle = DrivingSlotFixture.CreateVehicle("B456CD96", Guid.CreateVersion7());
        _fixture.Vehicles.Seed(vehicle);

        var result = await Offer(_start, DrivingInstructor(Guid.CreateVersion7()), vehicle.Id);

        result.Error.Should().Be(ApplicationErrors.NotFound<Vehicle>());
    }

    [Fact]
    public async Task Offer_VehicleOutOfService_ReturnsVehicleInactive()
    {
        _fixture.Vehicle.Deactivate();

        var result = await Offer(_start);

        result.Error.Should().Be(DomainErrors.DrivingSlot.VehicleInactive);
    }

    [Fact]
    public async Task Offer_GroundNoLongerUsed_ReturnsTrainingGroundInactive()
    {
        _fixture.Ground.Deactivate();

        var result = await Offer(_start, groundId: _fixture.Ground.Id);

        result.Error.Should().Be(DomainErrors.DrivingSlot.TrainingGroundInactive);
    }

    [Fact]
    public async Task Offer_RouteFromAGroundOfAnotherBranch_ReturnsDrivingRouteNotFound()
    {
        var ground = TrainingGround.Create("Far ground", Address.Create("Tyumen", "Respubliki 1").Value, null, Guid.CreateVersion7()).Value;
        _fixture.Grounds.Seed(ground);
        var route = DrivingRoute.Create("Far route", "Bridges", Guid.CreateVersion7(), ground.Id).Value;
        _fixture.Routes.Seed(route);

        var result = await Offer(_start, DrivingInstructor(Guid.CreateVersion7()), routeId: route.Id);

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingRoute>());
    }

    [Fact]
    public async Task Offer_InThePast_ReturnsStartInPast()
    {
        var result = await Offer(TestData.Now.AddHours(-1));

        result.Error.Should().Be(DomainErrors.DrivingSlot.StartInPast);
    }

    [Fact]
    public async Task Withdraw_OwnBookedSlot_WithdrawsItAndCancelsTheBooking()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await new WithdrawDrivingSlotCommandHandler(
                new DrivingSlotAccess(_fixture.Slots, _fixture.Instructor), _fixture.Instructor, TestData.Clock())
            .Handle(new WithdrawDrivingSlotCommand(slot.Id, "Car in repair"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Withdrawn);
        slot.Bookings.Should().ContainSingle().Which.Status.Should().Be(DrivingBookingStatus.Cancelled);
    }

    [Fact]
    public async Task Withdraw_SlotOfAnotherInstructor_ReturnsDrivingSlotNotFound()
    {
        var slot = _fixture.SeedSlot();
        var stranger = DrivingInstructor(null);

        var result = await new WithdrawDrivingSlotCommandHandler(
                new DrivingSlotAccess(_fixture.Slots, stranger), stranger, TestData.Clock())
            .Handle(new WithdrawDrivingSlotCommand(slot.Id, "Mine now"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
        slot.Status.Should().Be(DrivingSlotStatus.Available);
    }

    [Fact]
    public async Task Book_OwnMatchingContract_BooksTheSlot()
    {
        var slot = _fixture.SeedSlot();

        var result = await Book(slot);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
        slot.CurrentBooking!.ContractId.Should().Be(_fixture.Contract.Id);
    }

    [Fact]
    public async Task Book_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var slot = _fixture.SeedSlot();
        var other = _fixture.SeedContract("other@example.com", Transmission.Manual);

        var result = await Book(slot, contractId: other.Id);

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
        slot.Status.Should().Be(DrivingSlotStatus.Available);
    }

    [Fact]
    public async Task Book_ContractForAnotherGearbox_ReturnsTransmissionMismatch()
    {
        var slot = _fixture.SeedSlot();
        var automatic = _fixture.SeedContract("auto@example.com", Transmission.Automatic);
        var student = new FakeCurrentUser { UserId = automatic.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };

        var result = await Book(slot, student, automatic.Id);

        result.Error.Should().Be(DomainErrors.DrivingSlot.TransmissionMismatch);
    }

    [Fact]
    public async Task Book_StudentHasAnOverlappingLesson_ReturnsStudentBusy()
    {
        var otherVehicle = DrivingSlotFixture.CreateVehicle("B456CD96", null);
        _fixture.SeedSlot(_start.AddHours(1), otherVehicle, Guid.CreateVersion7()).Book(_fixture.Contract, TestData.Now);
        var slot = _fixture.SeedSlot();

        var result = await Book(slot);

        result.Error.Should().Be(ApplicationErrors.DrivingSlot.StudentBusy);
    }

    [Fact]
    public async Task Book_AlreadyBookedSlot_ReturnsNotAvailable()
    {
        var slot = _fixture.SeedBookedSlot();
        var other = _fixture.SeedContract("other@example.com", Transmission.Manual);
        var student = new FakeCurrentUser { UserId = other.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };

        var result = await Book(slot, student, other.Id);

        result.Error.Should().Be(DomainErrors.DrivingSlot.NotAvailable);
    }

    [Fact]
    public async Task Cancel_StudentBeforeTheNotice_CancelsAndFreesTheSlot()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Cancel(slot, _fixture.Student);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DrivingSlotStatus.Available);
        slot.Bookings.Should().ContainSingle().Which.CancelledByUserId.Should().Be(_fixture.Student.UserId);
    }

    [Fact]
    public async Task Cancel_StudentWithinTheDefaultNotice_ReturnsTooLateToCancel()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Cancel(slot, _fixture.Student, _start.AddHours(-23));

        result.Error.Should().Be(DomainErrors.DrivingSlot.TooLateToCancel);
    }

    [Fact]
    public async Task Cancel_InstructorWithinTheNotice_Cancels()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Cancel(slot, _fixture.Instructor, _start.AddHours(-1));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_ManagerOfTheContractBranch_Cancels()
    {
        var branchId = Guid.CreateVersion7();
        var contract = _fixture.SeedContract("branch@example.com", Transmission.Manual, branchId);
        var slot = _fixture.SeedSlot();
        slot.Book(contract, TestData.Now);

        var result = await Cancel(slot, TestData.Manager(branchId), _start.AddHours(-1));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_ManagerOfAnotherBranch_ReturnsDrivingSlotNotFound()
    {
        var contract = _fixture.SeedContract("branch@example.com", Transmission.Manual, Guid.CreateVersion7());
        var slot = _fixture.SeedSlot();
        slot.Book(contract, TestData.Now);

        var result = await Cancel(slot, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
        slot.Status.Should().Be(DrivingSlotStatus.Booked);
    }

    [Fact]
    public async Task Cancel_AnotherStudent_ReturnsDrivingSlotNotFound()
    {
        var slot = _fixture.SeedBookedSlot();

        var result = await Cancel(slot, FakeCurrentUser.SignedIn(UserRole.Student));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
    }
}

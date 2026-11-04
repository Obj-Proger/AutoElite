using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Contracts;
using AutoElite.Application.DrivingSlots;
using AutoElite.Application.DrivingSlots.GetAvailableDrivingSlots;
using AutoElite.Application.DrivingSlots.GetDrivingSchedule;
using AutoElite.Application.DrivingSlots.GetDrivingSlot;
using AutoElite.Application.DrivingSlots.GetMyDrivingLessons;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.DrivingSlots;

public class DrivingSlotQueryHandlerTests
{
    private static readonly DateTime _from = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _to = _from.AddDays(7);
    private static readonly TimeInterval _period = TimeInterval.Create(_from, _to).Value;

    private readonly FakeDrivingSlotQueries _queries = new();
    private readonly FakeContractQueries _contracts = new();

    private static FakeCurrentUser DrivingInstructor() => FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

    private static FakeCurrentUser Student(Guid studentId) =>
        new() { UserId = studentId, Roles = new HashSet<UserRole> { UserRole.Student } };

    private static DrivingSlotItem Slot(Guid instructorId, Guid? branchId = null) => new(
        Guid.CreateVersion7(), instructorId, "Sidorov Sergey", Guid.CreateVersion7(), "A123BC66", "Lada Vesta", LicenseCategory.B,
        Transmission.Manual, branchId, null, null, null, null, null, _from.AddHours(10), _from.AddHours(12), DrivingSlotStatus.Available);

    private ContractDetails SeedContract(
        Guid studentId,
        Guid? branchId = null,
        ContractStatus status = ContractStatus.Active,
        Transmission transmission = Transmission.Automatic)
    {
        var details = new ContractDetails(
            Guid.CreateVersion7(), "AE-1", studentId, "Ivanov Ivan", Guid.CreateVersion7(), "Category B", LicenseCategory.B,
            transmission, branchId, null, status, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), null, null, "RUB",
            45000m, 10000m, 35000m, 0m, 56, 0, null, TestData.Now);
        _contracts.Seed(details);

        return details;
    }

    private Task<Result<IReadOnlyList<DrivingScheduleItem>>> GetSchedule(GetDrivingScheduleQuery query, FakeCurrentUser caller) =>
        new GetDrivingScheduleQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    private Task<Result<IReadOnlyList<DrivingSlotItem>>> GetAvailable(Guid contractId, FakeCurrentUser caller, Guid? instructorId = null) =>
        new GetAvailableDrivingSlotsQueryHandler(_queries, _contracts, caller, TestData.Clock())
            .Handle(new GetAvailableDrivingSlotsQuery(contractId, _from, _to, instructorId), TestContext.Current.CancellationToken);

    private Task<Result<DrivingSlotDetails>> GetSlot(Guid slotId, FakeCurrentUser caller) =>
        new GetDrivingSlotQueryHandler(_queries, caller).Handle(new GetDrivingSlotQuery(slotId), TestContext.Current.CancellationToken);

    private DrivingSlotDetails SeedDetails(Guid instructorId, Guid? branchId = null)
    {
        var details = new DrivingSlotDetails(Slot(instructorId, branchId), []);
        _queries.Seed(details);

        return details;
    }

    [Fact]
    public async Task GetSchedule_Manager_PassesTheFiltersWithTheScope()
    {
        var branchId = Guid.CreateVersion7();
        var instructorId = Guid.CreateVersion7();

        var result = await GetSchedule(
            new GetDrivingScheduleQuery(_from, _to, instructorId, DrivingSlotStatus.Booked), TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
        _queries.LastScheduleCall.Should().Be((
            new DrivingScheduleFilter(_period, instructorId, DrivingSlotStatus.Booked), DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task GetSchedule_Instructor_ReadsOnlyOwnSlots()
    {
        var instructor = DrivingInstructor();

        await GetSchedule(new GetDrivingScheduleQuery(_from, _to), instructor);

        _queries.LastScheduleCall!.Value.Filter.InstructorId.Should().Be(instructor.UserId!.Value);
    }

    [Fact]
    public async Task GetSchedule_InstructorAsksForAnotherInstructor_ReturnsAnEmptyScheduleWithoutReading()
    {
        var result = await GetSchedule(new GetDrivingScheduleQuery(_from, _to, Guid.CreateVersion7()), DrivingInstructor());

        result.Value.Should().BeEmpty();
        _queries.LastScheduleCall.Should().BeNull();
    }

    [Fact]
    public async Task GetSchedule_PeriodLongerThanTheLimit_ReturnsPeriodTooLong()
    {
        var result = await GetSchedule(new GetDrivingScheduleQuery(_from, _from.AddDays(93)), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Schedule.PeriodTooLong);
    }

    [Fact]
    public void ScheduleValidator_UndefinedStatus_ReportsStatusNotDefined()
    {
        var query = new GetDrivingScheduleQuery(_from, _to, Status: (DrivingSlotStatus)99);

        var result = new GetDrivingScheduleQueryValidator().Validate(query);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.DrivingSlot.StatusNotDefined.Code);
    }

    [Fact]
    public async Task GetAvailable_OwnActiveContract_MatchesItsCategoryGearboxAndBranch()
    {
        var studentId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var instructorId = Guid.CreateVersion7();
        var contract = SeedContract(studentId, branchId);

        var result = await GetAvailable(contract.Id, Student(studentId), instructorId);

        result.IsSuccess.Should().BeTrue();
        _queries.LastAvailableCall.Should().Be((
            new AvailableDrivingSlotFilter(_period, TestData.Now, LicenseCategory.B, Transmission.Automatic, instructorId),
            DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task GetAvailable_ContractWithoutABranch_ReadsTheWholeSchool()
    {
        var studentId = Guid.CreateVersion7();
        var contract = SeedContract(studentId);

        await GetAvailable(contract.Id, Student(studentId));

        _queries.LastAvailableCall!.Value.Scope.Should().Be(DataScope.SchoolWide);
    }

    [Fact]
    public async Task GetAvailable_ContractOfAnotherStudent_ReturnsContractNotFound()
    {
        var contract = SeedContract(Guid.CreateVersion7());

        var result = await GetAvailable(contract.Id, Student(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
        _queries.LastAvailableCall.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailable_ClosedContract_ReturnsNothingWithoutReading()
    {
        var studentId = Guid.CreateVersion7();
        var contract = SeedContract(studentId, status: ContractStatus.Completed);

        var result = await GetAvailable(contract.Id, Student(studentId));

        result.Value.Should().BeEmpty();
        _queries.LastAvailableCall.Should().BeNull();
    }

    [Fact]
    public async Task GetMyLessons_Student_ReadsOwnLessonsForThePeriod()
    {
        var studentId = Guid.CreateVersion7();

        var result = await new GetMyDrivingLessonsQueryHandler(_queries, Student(studentId))
            .Handle(new GetMyDrivingLessonsQuery(_from, _to), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _queries.LastStudentCall.Should().Be((studentId, _period));
    }

    [Fact]
    public async Task GetSlot_OwnSlotOfTheInstructor_ReadsTheCard()
    {
        var instructor = DrivingInstructor();
        var details = SeedDetails(instructor.UserId!.Value);

        var result = await GetSlot(details.Slot.Id, instructor);

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetSlot_ManagerOfTheVehicleBranch_ReadsTheCard()
    {
        var branchId = Guid.CreateVersion7();
        var details = SeedDetails(Guid.CreateVersion7(), branchId);

        var result = await GetSlot(details.Slot.Id, TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSlot_SlotOfAnotherInstructor_ReturnsDrivingSlotNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await GetSlot(details.Slot.Id, DrivingInstructor());

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
    }

    [Fact]
    public async Task GetSlot_ManagerOfAnotherBranch_ReturnsDrivingSlotNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await GetSlot(details.Slot.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingSlot>());
    }
}

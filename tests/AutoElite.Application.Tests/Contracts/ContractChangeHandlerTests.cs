using AutoElite.Application.Contracts;
using AutoElite.Application.Contracts.ChangeContractPrice;
using AutoElite.Application.Contracts.ChangeContractTransmission;
using AutoElite.Application.Contracts.CompleteContract;
using AutoElite.Application.Contracts.RescheduleContract;
using AutoElite.Application.Contracts.TerminateContract;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Contracts;

public class ContractChangeHandlerTests
{
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 6, 30);
    private static readonly DateOnly _today = DateOnly.FromDateTime(TestData.Now);

    private readonly FakeContractRepository _contracts = new();
    private readonly Course _course = TestData.NewCourse();

    private ContractAccess Access(FakeCurrentUser? caller = null) => new(_contracts, caller ?? TestData.Administrator());

    private Contract SeedContract(Guid? branchId = null)
    {
        var student = TestData.NewUser();
        var contract = Contract.Create("AE-1", student, _course, _course.Price, Transmission.Manual, branchId, _start, _end, TestData.Now)
            .Value;
        contract.ClearDomainEvents();
        _contracts.Seed(contract);

        return contract;
    }

    private Task<Result> Complete(Guid contractId, DateOnly completedOn) =>
        new CompleteContractCommandHandler(Access(), TestData.Clock())
            .Handle(new CompleteContractCommand(contractId, completedOn), TestContext.Current.CancellationToken);

    private Task<Result> Terminate(Guid contractId, string? reason, DateOnly terminatedOn, FakeCurrentUser? caller = null) =>
        new TerminateContractCommandHandler(Access(caller), TestData.Clock())
            .Handle(new TerminateContractCommand(contractId, reason, terminatedOn), TestContext.Current.CancellationToken);

    [Fact]
    public async Task ChangePrice_NewAmount_KeepsTheContractCurrency()
    {
        var contract = SeedContract();

        var result = await new ChangeContractPriceCommandHandler(Access())
            .Handle(new ChangeContractPriceCommand(contract.Id, 40000m), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        contract.Price.Should().Be(Money.Create(40000m, _course.Price.Currency).Value);
    }

    [Fact]
    public async Task ChangePrice_ZeroAmount_ReturnsPriceNotPositive()
    {
        var contract = SeedContract();

        var result = await new ChangeContractPriceCommandHandler(Access())
            .Handle(new ChangeContractPriceCommand(contract.Id, 0m), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Contract.PriceNotPositive);
    }

    [Fact]
    public async Task ChangeTransmission_OtherGearbox_SwitchesIt()
    {
        var contract = SeedContract();

        var result = await new ChangeContractTransmissionCommandHandler(Access())
            .Handle(new ChangeContractTransmissionCommand(contract.Id, Transmission.Automatic), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        contract.Transmission.Should().Be(Transmission.Automatic);
    }

    [Fact]
    public async Task Reschedule_NewPeriod_ChangesTheDates()
    {
        var contract = SeedContract();
        var newEnd = _end.AddMonths(1);

        var result = await new RescheduleContractCommandHandler(Access())
            .Handle(new RescheduleContractCommand(contract.Id, _start, newEnd), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        contract.EndDate.Should().Be(newEnd);
    }

    [Fact]
    public async Task Complete_PaidContract_CompletesIt()
    {
        var contract = SeedContract();
        contract.ApplyPayment(contract.Price);

        var result = await Complete(contract.Id, _today);

        result.IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Completed);
        contract.ClosedOn.Should().Be(_today);
        contract.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ContractClosedDomainEvent>();
    }

    [Fact]
    public async Task Complete_ContractWithDebt_ReturnsOutstandingDebt()
    {
        var contract = SeedContract();

        var result = await Complete(contract.Id, _today);

        result.Error.Should().Be(DomainErrors.Contract.OutstandingDebt);
        contract.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public async Task Complete_TomorrowInUtc_IsAllowedForBranchesEastOfUtc()
    {
        var contract = SeedContract();
        contract.ApplyPayment(contract.Price);

        var result = await Complete(contract.Id, _today.AddDays(1));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Complete_DayAfterTomorrow_ReturnsClosedInFuture()
    {
        var contract = SeedContract();
        contract.ApplyPayment(contract.Price);

        var result = await Complete(contract.Id, _today.AddDays(2));

        result.Error.Should().Be(ApplicationErrors.Contract.ClosedInFuture);
        contract.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public async Task Terminate_WithReason_TerminatesTheContract()
    {
        var contract = SeedContract();

        var result = await Terminate(contract.Id, "Moved to another city", _today);

        result.IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Terminated);
        contract.TerminationReason.Should().Be("Moved to another city");
    }

    [Fact]
    public async Task Terminate_WithoutReason_ReturnsTerminationReasonEmpty()
    {
        var contract = SeedContract();

        var result = await Terminate(contract.Id, "", _today);

        result.Error.Should().Be(DomainErrors.Contract.TerminationReasonEmpty);
    }

    [Fact]
    public async Task Terminate_ContractOfAnotherBranch_ReturnsContractNotFoundToTheManager()
    {
        var contract = SeedContract(Guid.CreateVersion7());

        var result = await Terminate(contract.Id, "Moved", _today, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
        contract.Status.Should().Be(ContractStatus.Active);
    }
}

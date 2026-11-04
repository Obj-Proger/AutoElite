using AutoElite.Application.Contracts;
using AutoElite.Application.Payments;
using AutoElite.Application.Payments.CancelPayment;
using AutoElite.Application.Payments.ConfirmPayment;
using AutoElite.Application.Payments.RefundPayment;
using AutoElite.Application.Payments.RegisterPayment;
using AutoElite.Application.Payments.SetPaymentReference;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Payments;

public class PaymentCommandHandlerTests
{
    private readonly FakeContractRepository _contracts = new();
    private readonly InMemoryRepository<Payment> _payments = new();
    private readonly FakeCurrentUser _manager = TestData.Manager(null);
    private readonly Course _course = TestData.NewCourse();

    private ContractAccess CreateContractAccess(FakeCurrentUser? caller = null) => new(_contracts, caller ?? _manager);

    private PaymentAccess CreatePaymentAccess(FakeCurrentUser? caller = null) => new(_payments, CreateContractAccess(caller));

    private Contract SeedContract(Guid? branchId = null)
    {
        var contract = Contract.Create(
                "AE-1", TestData.NewUser(), _course, _course.Price, Transmission.Manual, branchId,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        _contracts.Seed(contract);

        return contract;
    }

    private Task<Result<Guid>> Register(Guid contractId, decimal amount = 15000m, bool confirmNow = false) =>
        new RegisterPaymentCommandHandler(_payments, CreateContractAccess(), _manager, TestData.Clock()).Handle(
            new RegisterPaymentCommand(contractId, amount, PaymentMethod.Cash, "Tuition, first instalment", null, confirmNow),
            TestContext.Current.CancellationToken);

    private Task<Result> Confirm(Guid paymentId) =>
        new ConfirmPaymentCommandHandler(CreatePaymentAccess(), TestData.Clock())
            .Handle(new ConfirmPaymentCommand(paymentId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Register_Payment_RecordsItInTheContractCurrencyWithoutChangingTheDebt()
    {
        var contract = SeedContract();

        var result = await Register(contract.Id);

        var payment = _payments.Items.Should().ContainSingle().Which;
        payment.Id.Should().Be(result.Value);
        payment.Amount.Should().Be(Money.Create(15000m, _course.Price.Currency).Value);
        payment.Status.Should().Be(PaymentStatus.Registered);
        payment.RegisteredByUserId.Should().Be(_manager.UserId);
        contract.PaidAmount.IsZero.Should().BeTrue();
    }

    [Fact]
    public async Task Register_ConfirmNow_ConfirmsAndAddsToThePaidAmount()
    {
        var contract = SeedContract();

        await Register(contract.Id, confirmNow: true);

        _payments.Items.Single().Status.Should().Be(PaymentStatus.Confirmed);
        contract.PaidAmount.Amount.Should().Be(15000m);
    }

    [Fact]
    public async Task Register_ZeroAmount_ReturnsAmountNotPositive()
    {
        var contract = SeedContract();

        var result = await Register(contract.Id, 0m);

        result.Error.Should().Be(DomainErrors.Payment.AmountNotPositive);
        _payments.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_ContractOfAnotherBranch_ReturnsContractNotFound()
    {
        var contract = SeedContract(Guid.CreateVersion7());

        var result = await new RegisterPaymentCommandHandler(
                _payments, CreateContractAccess(TestData.Manager(Guid.CreateVersion7())), _manager, TestData.Clock())
            .Handle(new RegisterPaymentCommand(contract.Id, 100m, PaymentMethod.Cash, "Tuition"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task Confirm_RegisteredPayment_AddsToThePaidAmount()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id)).Value;

        var result = await Confirm(paymentId);

        result.IsSuccess.Should().BeTrue();
        contract.Debt.Amount.Should().Be(_course.Price.Amount - 15000m);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmedPayment_ReturnsNotRegistered()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id, confirmNow: true)).Value;

        var result = await Confirm(paymentId);

        result.Error.Should().Be(DomainErrors.Payment.NotRegistered);
        contract.PaidAmount.Amount.Should().Be(15000m);
    }

    [Fact]
    public async Task Confirm_PaymentOfContractOutsideTheScope_ReturnsPaymentNotFound()
    {
        var contract = SeedContract(Guid.CreateVersion7());
        var payment = Payment.Register(
            contract, Money.Create(100m, _course.Price.Currency).Value, PaymentMethod.Cash, "Tuition", null, null, TestData.Now).Value;
        _payments.Seed(payment);

        var result = await new ConfirmPaymentCommandHandler(CreatePaymentAccess(TestData.Manager(Guid.CreateVersion7())), TestData.Clock())
            .Handle(new ConfirmPaymentCommand(payment.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Payment>());
    }

    [Fact]
    public async Task Refund_ConfirmedPayment_TakesItOffThePaidAmount()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id, confirmNow: true)).Value;

        var result = await new RefundPaymentCommandHandler(CreatePaymentAccess(), TestData.Clock())
            .Handle(new RefundPaymentCommand(paymentId, "Course cancelled"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _payments.Items.Single().Status.Should().Be(PaymentStatus.Refunded);
        contract.PaidAmount.IsZero.Should().BeTrue();
    }

    [Fact]
    public async Task Refund_RegisteredPayment_ReturnsNotConfirmed()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id)).Value;

        var result = await new RefundPaymentCommandHandler(CreatePaymentAccess(), TestData.Clock())
            .Handle(new RefundPaymentCommand(paymentId, "Mistake"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Payment.NotConfirmed);
    }

    [Fact]
    public async Task Cancel_RegisteredPayment_CancelsItWithTheReason()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id)).Value;

        var result = await new CancelPaymentCommandHandler(CreatePaymentAccess(), TestData.Clock())
            .Handle(new CancelPaymentCommand(paymentId, "Recorded twice"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var payment = _payments.Items.Single();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.Reason.Should().Be("Recorded twice");
    }

    [Fact]
    public async Task SetReference_RegisteredPayment_RecordsIt()
    {
        var contract = SeedContract();
        var paymentId = (await Register(contract.Id)).Value;

        var result = await new SetPaymentReferenceCommandHandler(CreatePaymentAccess())
            .Handle(new SetPaymentReferenceCommand(paymentId, "BANK-2026-0042"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _payments.Items.Single().ExternalReference.Should().Be("BANK-2026-0042");
    }

    [Fact]
    public async Task UnknownPayment_ReturnsPaymentNotFound()
    {
        var result = await Confirm(Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<Payment>());
    }
}

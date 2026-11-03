namespace AutoElite.Domain.Tests.Entities;

public class PaymentTests
{
    private static readonly DateTime _now = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = new(2026, 2, 3, 15, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _managerId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Currency _usd = Currency.Create("USD").Value;
    private static readonly Course _course =
        Course.Create("Category B", LicenseCategory.B, Rub(45000m), 134, 56, null).Value;

    private static Money Rub(decimal amount) => Money.Create(amount, _rub).Value;

    private static Contract CreateContract()
    {
        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;

        return Contract.Create("AE-1", student, _course, Rub(45000m), null, _start, _end, _now).Value;
    }

    private static Payment Register(Contract contract, decimal amount = 15000m) =>
        Payment.Register(contract, Rub(amount), PaymentMethod.Cash, "Tuition, first instalment", null, _managerId, _now).Value;

    private static Payment RegisterConfirmed(Contract contract, decimal amount = 15000m)
    {
        var payment = Register(contract, amount);
        payment.Confirm(contract, _now);

        return payment;
    }

    [Fact]
    public void Register_ValidData_ReturnsRegisteredPaymentWithGivenValues()
    {
        var contract = CreateContract();

        var result = Payment.Register(contract, Rub(15000m), PaymentMethod.BankCard, "  First instalment  ", "  TX-42  ", _managerId, _now);

        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        payment.ContractId.Should().Be(contract.Id);
        payment.StudentId.Should().Be(contract.StudentId);
        payment.Amount.Should().Be(Rub(15000m));
        payment.Method.Should().Be(PaymentMethod.BankCard);
        payment.Purpose.Should().Be("First instalment");
        payment.ExternalReference.Should().Be("TX-42");
        payment.RegisteredByUserId.Should().Be(_managerId);
        payment.RegisteredAtUtc.Should().Be(_now);
        payment.Status.Should().Be(PaymentStatus.Registered);
        payment.ConfirmedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Register_Payment_DoesNotChangeContract()
    {
        var contract = CreateContract();

        Register(contract);

        contract.PaidAmount.Should().Be(Money.Zero(_rub));
    }

    [Fact]
    public void Register_OnlinePaymentWithoutStaff_ReturnsPayment()
    {
        var result = Payment.Register(CreateContract(), Rub(15000m), PaymentMethod.Online, "Tuition", null, null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.RegisteredByUserId.Should().BeNull();
    }

    [Fact]
    public void Register_TerminatedContract_ReturnsPayment()
    {
        var contract = CreateContract();
        contract.Terminate("Moved away", _start, _now);

        var result = Payment.Register(contract, Rub(1000m), PaymentMethod.Cash, "Settling the debt", null, _managerId, _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Register_OtherCurrency_ReturnsCurrencyMismatchError()
    {
        var result = Payment.Register(CreateContract(), Money.Create(100m, _usd).Value, PaymentMethod.Cash, "Tuition", null, _managerId, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.CurrencyMismatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Register_AmountNotPositive_ReturnsAmountNotPositiveError(int amount)
    {
        var result = Payment.Register(CreateContract(), Rub(amount), PaymentMethod.Cash, "Tuition", null, _managerId, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.AmountNotPositive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Register_UndefinedMethod_ReturnsMethodNotDefinedError(int rawMethod)
    {
        var result = Payment.Register(CreateContract(), Rub(100m), (PaymentMethod)rawMethod, "Tuition", null, _managerId, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.MethodNotDefined);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_EmptyPurpose_ReturnsPurposeEmptyError(string? purpose)
    {
        var result = Payment.Register(CreateContract(), Rub(100m), PaymentMethod.Cash, purpose, null, _managerId, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.PurposeEmpty);
    }

    [Fact]
    public void Register_PurposeTooLong_ReturnsPurposeTooLongError()
    {
        var result = Payment.Register(CreateContract(), Rub(100m), PaymentMethod.Cash, new string('a', 501), null, _managerId, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.PurposeTooLong);
    }

    [Fact]
    public void Register_ExternalReferenceTooLong_ReturnsExternalReferenceTooLongError()
    {
        var result = Payment.Register(CreateContract(), Rub(100m), PaymentMethod.Online, "Tuition", new string('a', 201), null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ExternalReferenceTooLong);
    }

    [Fact]
    public void Register_EmptyUserId_ReturnsUserIdEmptyError()
    {
        var result = Payment.Register(CreateContract(), Rub(100m), PaymentMethod.Cash, "Tuition", null, Guid.Empty, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.UserIdEmpty);
    }

    [Fact]
    public void Confirm_RegisteredPayment_ConfirmsAndAddsAmountToContract()
    {
        var contract = CreateContract();
        var payment = Register(contract);

        var result = payment.Confirm(contract, _later);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ConfirmedAtUtc.Should().Be(_later);
        contract.PaidAmount.Should().Be(Rub(15000m));
        contract.Debt.Should().Be(Rub(30000m));
    }

    [Fact]
    public void Confirm_RegisteredPayment_RaisesPaymentConfirmedEvent()
    {
        var contract = CreateContract();
        var payment = Register(contract);

        payment.Confirm(contract, _later);

        payment.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new PaymentConfirmedDomainEvent(payment.Id, contract.Id, contract.StudentId, 15000m, "RUB", _later));
    }

    [Fact]
    public void Confirm_ConfirmedPayment_ReturnsNotRegisteredErrorAndCountsOnce()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.Confirm(contract, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotRegistered);
        contract.PaidAmount.Should().Be(Rub(15000m));
    }

    [Fact]
    public void Confirm_CancelledPayment_ReturnsNotRegisteredError()
    {
        var contract = CreateContract();
        var payment = Register(contract);
        payment.Cancel("Entered twice", _now);

        var result = payment.Confirm(contract, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotRegistered);
        contract.PaidAmount.Should().Be(Money.Zero(_rub));
    }

    [Fact]
    public void Confirm_OtherContract_ReturnsContractMismatchErrorWithoutChanges()
    {
        var contract = CreateContract();
        var otherContract = CreateContract();
        var payment = Register(contract);

        var result = payment.Confirm(otherContract, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ContractMismatch);
        payment.Status.Should().Be(PaymentStatus.Registered);
        otherContract.PaidAmount.Should().Be(Money.Zero(_rub));
    }

    [Fact]
    public void Refund_ConfirmedPayment_RefundsAndTakesAmountOffContract()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.Refund(contract, "  Contract terminated  ", _later);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAtUtc.Should().Be(_later);
        payment.Reason.Should().Be("Contract terminated");
        contract.PaidAmount.Should().Be(Money.Zero(_rub));
    }

    [Fact]
    public void Refund_ConfirmedPayment_RaisesPaymentRefundedEvent()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);
        payment.ClearDomainEvents();

        payment.Refund(contract, "Contract terminated", _later);

        payment.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new PaymentRefundedDomainEvent(payment.Id, contract.Id, contract.StudentId, 15000m, "RUB", _later));
    }

    [Fact]
    public void Refund_RegisteredPayment_ReturnsNotConfirmedError()
    {
        var contract = CreateContract();
        var payment = Register(contract);

        var result = payment.Refund(contract, "Contract terminated", _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotConfirmed);
    }

    [Fact]
    public void Refund_RefundedPayment_ReturnsNotConfirmedErrorAndRefundsOnce()
    {
        var contract = CreateContract();
        RegisterConfirmed(contract, 10000m);
        var payment = RegisterConfirmed(contract, 15000m);
        payment.Refund(contract, "Contract terminated", _now);

        var result = payment.Refund(contract, "Again", _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotConfirmed);
        contract.PaidAmount.Should().Be(Rub(10000m));
    }

    [Fact]
    public void Refund_OtherContract_ReturnsContractMismatchError()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.Refund(CreateContract(), "Contract terminated", _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ContractMismatch);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Refund_EmptyReason_ReturnsReasonEmptyErrorWithoutChanges(string? reason)
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.Refund(contract, reason, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ReasonEmpty);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        contract.PaidAmount.Should().Be(Rub(15000m));
    }

    [Fact]
    public void Cancel_RegisteredPayment_CancelsWithReason()
    {
        var contract = CreateContract();
        var payment = Register(contract);

        var result = payment.Cancel("  Entered twice  ", _later);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CancelledAtUtc.Should().Be(_later);
        payment.Reason.Should().Be("Entered twice");
    }

    [Fact]
    public void Cancel_ConfirmedPayment_ReturnsNotRegisteredError()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.Cancel("Entered twice", _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotRegistered);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
    }

    [Fact]
    public void Cancel_ReasonTooLong_ReturnsReasonTooLongError()
    {
        var payment = Register(CreateContract());

        var result = payment.Cancel(new string('a', 1001), _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ReasonTooLong);
    }

    [Fact]
    public void SetExternalReference_RegisteredPayment_SetsTrimmedReference()
    {
        var payment = Register(CreateContract());

        var result = payment.SetExternalReference("  gateway-123  ");

        result.IsSuccess.Should().BeTrue();
        payment.ExternalReference.Should().Be("gateway-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void SetExternalReference_Empty_ReturnsExternalReferenceEmptyError(string? reference)
    {
        var payment = Register(CreateContract());

        var result = payment.SetExternalReference(reference);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.ExternalReferenceEmpty);
    }

    [Fact]
    public void SetExternalReference_ConfirmedPayment_ReturnsNotRegisteredError()
    {
        var contract = CreateContract();
        var payment = RegisterConfirmed(contract);

        var result = payment.SetExternalReference("gateway-123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Payment.NotRegistered);
    }
}

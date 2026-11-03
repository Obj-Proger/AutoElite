using System.Globalization;

namespace AutoElite.Domain.Tests.Entities;

public class ContractTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Currency _usd = Currency.Create("USD").Value;
    private static readonly User _student =
        User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;
    private static readonly Course _course =
        Course.Create("Category B", LicenseCategory.B, Rub(45000m), 134, 56, null).Value;

    private static Money Rub(decimal amount) => Money.Create(amount, _rub).Value;

    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static Contract CreateContract(decimal price = 45000m) =>
        Contract.Create("AE-2026-001", _student, _course, Rub(price), _branchId, _start, _end, _now).Value;

    private static Contract CreateTerminatedContract()
    {
        var contract = CreateContract();
        contract.Terminate("Moved to another city", _start, _now);

        return contract;
    }

    [Fact]
    public void Create_ValidData_ReturnsActiveContractWithGivenValues()
    {
        var result = Contract.Create("  AE-2026-001  ", _student, _course, Rub(40000m), _branchId, _start, _end, _now);

        result.IsSuccess.Should().BeTrue();
        var contract = result.Value;
        contract.Number.Should().Be("AE-2026-001");
        contract.StudentId.Should().Be(_student.Id);
        contract.CourseId.Should().Be(_course.Id);
        contract.Category.Should().Be(LicenseCategory.B);
        contract.BranchId.Should().Be(_branchId);
        contract.StartDate.Should().Be(_start);
        contract.EndDate.Should().Be(_end);
        contract.Price.Should().Be(Rub(40000m));
        contract.PaidAmount.Should().Be(Money.Zero(_rub));
        contract.Debt.Should().Be(Rub(40000m));
        contract.CompletedPracticeHours.Should().Be(0m);
        contract.AttendedTheoryLessons.Should().Be(0);
        contract.QualityScore.Should().BeNull();
        contract.Status.Should().Be(ContractStatus.Active);
        contract.ClosedOn.Should().BeNull();
    }

    [Fact]
    public void Create_ValidData_RaisesContractCreatedEvent()
    {
        var contract = CreateContract();

        contract.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ContractCreatedDomainEvent(contract.Id, _student.Id, _course.Id, _now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyNumber_ReturnsNumberEmptyError(string? number)
    {
        var result = Contract.Create(number, _student, _course, Rub(45000m), null, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NumberEmpty);
    }

    [Fact]
    public void Create_NumberTooLong_ReturnsNumberTooLongError()
    {
        var result = Contract.Create(new string('1', 51), _student, _course, Rub(45000m), null, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NumberTooLong);
    }

    [Fact]
    public void Create_InactiveStudent_ReturnsStudentInactiveError()
    {
        var student = User.Create(FullName.Create("Petrov", "Petr").Value, Email.Create("petr@example.com").Value, null, null, _now).Value;
        student.Deactivate(_now);

        var result = Contract.Create("AE-1", student, _course, Rub(45000m), null, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.StudentInactive);
    }

    [Fact]
    public void Create_InactiveCourse_ReturnsCourseInactiveError()
    {
        var course = Course.Create("Category A", LicenseCategory.A, Rub(30000m), 130, 18, null).Value;
        course.Deactivate();

        var result = Contract.Create("AE-1", _student, course, Rub(30000m), null, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.CourseInactive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_PriceNotPositive_ReturnsPriceNotPositiveError(int amount)
    {
        var result = Contract.Create("AE-1", _student, _course, Rub(amount), null, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.PriceNotPositive);
    }

    [Fact]
    public void Create_PriceInOtherCurrencyThanCourse_ReturnsContractInThatCurrency()
    {
        var result = Contract.Create("AE-1", _student, _course, Money.Create(500m, _usd).Value, null, _start, _end, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaidAmount.Should().Be(Money.Zero(_usd));
    }

    [Fact]
    public void Create_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = Contract.Create("AE-1", _student, _course, Rub(45000m), Guid.Empty, _start, _end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.BranchIdEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_EndNotAfterStart_ReturnsEndNotAfterStartError(int daysAfterStart)
    {
        var result = Contract.Create("AE-1", _student, _course, Rub(45000m), null, _start, _start.AddDays(daysAfterStart), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.EndNotAfterStart);
    }

    [Fact]
    public void ChangePrice_SameCurrency_ChangesPriceAndDebt()
    {
        var contract = CreateContract();

        var result = contract.ChangePrice(Rub(40500m));

        result.IsSuccess.Should().BeTrue();
        contract.Price.Should().Be(Rub(40500m));
        contract.Debt.Should().Be(Rub(40500m));
    }

    [Fact]
    public void ChangePrice_OtherCurrency_ReturnsCurrencyMismatchError()
    {
        var contract = CreateContract();

        var result = contract.ChangePrice(Money.Create(500m, _usd).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.CurrencyMismatch);
        contract.Price.Should().Be(Rub(45000m));
    }

    [Fact]
    public void ChangePrice_NotPositive_ReturnsPriceNotPositiveError()
    {
        var contract = CreateContract();

        var result = contract.ChangePrice(Money.Zero(_rub));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.PriceNotPositive);
    }

    [Fact]
    public void ChangePrice_TerminatedContract_ReturnsNotActiveError()
    {
        var contract = CreateTerminatedContract();

        var result = contract.ChangePrice(Rub(40000m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NotActive);
    }

    [Fact]
    public void Reschedule_ValidPeriod_ChangesDates()
    {
        var contract = CreateContract();
        var newEnd = new DateOnly(2026, 6, 30);

        var result = contract.Reschedule(_start, newEnd);

        result.IsSuccess.Should().BeTrue();
        contract.EndDate.Should().Be(newEnd);
    }

    [Fact]
    public void Reschedule_EndNotAfterStart_ReturnsEndNotAfterStartError()
    {
        var contract = CreateContract();

        var result = contract.Reschedule(_end, _start);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.EndNotAfterStart);
        contract.StartDate.Should().Be(_start);
    }

    [Fact]
    public void ApplyPayment_PositiveAmount_IncreasesPaidAmountAndReducesDebt()
    {
        var contract = CreateContract();

        var result = contract.ApplyPayment(Rub(15000m));

        result.IsSuccess.Should().BeTrue();
        contract.PaidAmount.Should().Be(Rub(15000m));
        contract.Debt.Should().Be(Rub(30000m));
    }

    [Fact]
    public void ApplyPayment_MoreThanPrice_LeavesNegativeDebt()
    {
        var contract = CreateContract();

        contract.ApplyPayment(Rub(46000m));

        contract.Debt.Should().Be(Rub(-1000m));
    }

    [Fact]
    public void ApplyPayment_TerminatedContract_IsAccepted()
    {
        var contract = CreateTerminatedContract();

        var result = contract.ApplyPayment(Rub(1000m));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ApplyPayment_OtherCurrency_ReturnsCurrencyMismatchError()
    {
        var contract = CreateContract();

        var result = contract.ApplyPayment(Money.Create(100m, _usd).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.CurrencyMismatch);
        contract.PaidAmount.Should().Be(Money.Zero(_rub));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ApplyPayment_AmountNotPositive_ReturnsAmountNotPositiveError(int amount)
    {
        var contract = CreateContract();

        var result = contract.ApplyPayment(Rub(amount));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.AmountNotPositive);
    }

    [Fact]
    public void ApplyRefund_UpToPaidAmount_ReducesPaidAmount()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(15000m));

        var result = contract.ApplyRefund(Rub(15000m));

        result.IsSuccess.Should().BeTrue();
        contract.PaidAmount.Should().Be(Money.Zero(_rub));
        contract.Debt.Should().Be(Rub(45000m));
    }

    [Fact]
    public void ApplyRefund_MoreThanPaidAmount_ReturnsRefundExceedsPaidAmountError()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(15000m));

        var result = contract.ApplyRefund(Rub(15000.01m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.RefundExceedsPaidAmount);
        contract.PaidAmount.Should().Be(Rub(15000m));
    }

    [Fact]
    public void ApplyRefund_OtherCurrency_ReturnsCurrencyMismatchError()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(15000m));

        var result = contract.ApplyRefund(Money.Create(100m, _usd).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.CurrencyMismatch);
    }

    [Fact]
    public void RecordPracticeLesson_ValidHoursAndGrade_AddsHoursAndGrade()
    {
        var contract = CreateContract();

        var result = contract.RecordPracticeLesson(1.5m, 4.33m);

        result.IsSuccess.Should().BeTrue();
        contract.CompletedPracticeHours.Should().Be(1.5m);
        contract.RecentGrades.Should().Equal(4.33m);
        contract.QualityScore.Should().Be(4.33m);
    }

    [Fact]
    public void RecordPracticeLesson_WithoutGrade_AddsOnlyHours()
    {
        var contract = CreateContract();

        contract.RecordPracticeLesson(2m, null);

        contract.CompletedPracticeHours.Should().Be(2m);
        contract.QualityScore.Should().BeNull();
    }

    [Fact]
    public void RecordPracticeLesson_SeveralLessons_AccumulatesHoursAndAveragesGrades()
    {
        var contract = CreateContract();
        contract.RecordPracticeLesson(1.5m, 3m);
        contract.RecordPracticeLesson(1.5m, 4m);

        contract.RecordPracticeLesson(2m, 4m);

        contract.CompletedPracticeHours.Should().Be(5m);
        contract.QualityScore.Should().Be(3.67m);
    }

    [Fact]
    public void RecordPracticeLesson_MoreGradesThanWindow_KeepsOnlyTheMostRecent()
    {
        var contract = CreateContract();
        contract.RecordPracticeLesson(1m, 1m);
        for (var lesson = 0; lesson < Contract.QualityWindowSize; lesson++)
            contract.RecordPracticeLesson(1m, 5m);

        var score = contract.QualityScore;

        contract.RecentGrades.Should().HaveCount(10).And.OnlyContain(grade => grade == 5m);
        score.Should().Be(5m);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("12.01")]
    [InlineData("1.255")]
    public void RecordPracticeLesson_HoursOutOfRange_ReturnsLessonHoursOutOfRangeError(string hours)
    {
        var contract = CreateContract();

        var result = contract.RecordPracticeLesson(Dec(hours), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.LessonHoursOutOfRange);
        contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public void RecordPracticeLesson_HoursAtMaximum_IsAccepted()
    {
        var contract = CreateContract();

        var result = contract.RecordPracticeLesson(Contract.MaxLessonHours, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("5.01")]
    [InlineData("4.333")]
    public void RecordPracticeLesson_GradeOutOfRange_ReturnsGradeOutOfRangeErrorWithoutAddingHours(string grade)
    {
        var contract = CreateContract();

        var result = contract.RecordPracticeLesson(1m, Dec(grade));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.GradeOutOfRange);
        contract.CompletedPracticeHours.Should().Be(0m);
    }

    [Fact]
    public void RecordPracticeLesson_TerminatedContract_ReturnsNotActiveError()
    {
        var contract = CreateTerminatedContract();

        var result = contract.RecordPracticeLesson(1m, 4m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NotActive);
    }

    [Fact]
    public void RecordTheoryAttendance_ActiveContract_IncrementsCounter()
    {
        var contract = CreateContract();

        var result = contract.RecordTheoryAttendance();

        result.IsSuccess.Should().BeTrue();
        contract.AttendedTheoryLessons.Should().Be(1);
    }

    [Fact]
    public void RevokeTheoryAttendance_AfterAttendance_DecrementsCounter()
    {
        var contract = CreateContract();
        contract.RecordTheoryAttendance();

        var result = contract.RevokeTheoryAttendance();

        result.IsSuccess.Should().BeTrue();
        contract.AttendedTheoryLessons.Should().Be(0);
    }

    [Fact]
    public void RevokeTheoryAttendance_NoAttendance_ReturnsNoTheoryAttendanceError()
    {
        var contract = CreateContract();

        var result = contract.RevokeTheoryAttendance();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NoTheoryAttendance);
    }

    [Fact]
    public void RecordTheoryAttendance_TerminatedContract_ReturnsNotActiveError()
    {
        var contract = CreateTerminatedContract();

        var result = contract.RecordTheoryAttendance();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NotActive);
    }

    [Fact]
    public void Complete_FullyPaid_ClosesContractAndRaisesContractClosedEvent()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(45000m));
        contract.ClearDomainEvents();

        var result = contract.Complete(_end, _now);

        result.IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Completed);
        contract.ClosedOn.Should().Be(_end);
        contract.TerminationReason.Should().BeNull();
        contract.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ContractClosedDomainEvent(contract.Id, _student.Id, ContractStatus.Completed, _now));
    }

    [Fact]
    public void Complete_Overpaid_IsAllowed()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(46000m));

        var result = contract.Complete(_end, _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Complete_WithDebt_ReturnsOutstandingDebtError()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(44999.99m));

        var result = contract.Complete(_end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.OutstandingDebt);
        contract.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public void Complete_BeforeStart_ReturnsClosedBeforeStartError()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(45000m));

        var result = contract.Complete(_start.AddDays(-1), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.ClosedBeforeStart);
    }

    [Fact]
    public void Complete_CompletedContract_ReturnsNotActiveError()
    {
        var contract = CreateContract();
        contract.ApplyPayment(Rub(45000m));
        contract.Complete(_end, _now);

        var result = contract.Complete(_end, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NotActive);
    }

    [Fact]
    public void Terminate_WithReason_ClosesContractAndRaisesContractClosedEvent()
    {
        var contract = CreateContract();
        contract.ClearDomainEvents();

        var result = contract.Terminate("  Moved to another city  ", _start.AddDays(10), _now);

        result.IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Terminated);
        contract.ClosedOn.Should().Be(_start.AddDays(10));
        contract.TerminationReason.Should().Be("Moved to another city");
        contract.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ContractClosedDomainEvent(contract.Id, _student.Id, ContractStatus.Terminated, _now));
    }

    [Fact]
    public void Terminate_WithDebt_IsAllowed()
    {
        var contract = CreateContract();

        var result = contract.Terminate("Stopped paying", _start, _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Terminate_EmptyReason_ReturnsTerminationReasonEmptyError(string? reason)
    {
        var contract = CreateContract();

        var result = contract.Terminate(reason, _start, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.TerminationReasonEmpty);
        contract.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public void Terminate_ReasonTooLong_ReturnsTerminationReasonTooLongError()
    {
        var contract = CreateContract();

        var result = contract.Terminate(new string('a', 1001), _start, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.TerminationReasonTooLong);
    }

    [Fact]
    public void Terminate_BeforeStart_ReturnsClosedBeforeStartError()
    {
        var contract = CreateContract();

        var result = contract.Terminate("Changed mind", _start.AddDays(-1), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.ClosedBeforeStart);
    }

    [Fact]
    public void Terminate_TerminatedContract_ReturnsNotActiveError()
    {
        var contract = CreateTerminatedContract();

        var result = contract.Terminate("Again", _start, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Contract.NotActive);
    }
}

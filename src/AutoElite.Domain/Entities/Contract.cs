using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A training contract between the school and a student for one course: the agreed price and what has been paid,
/// the training progress (practice hours, attended theory lessons, quality score) and the contract status.
/// </summary>
/// <remarks>
/// <para>
/// The price is copied from the course when the contract is drawn up and may be changed (for example, a discount);
/// later changes to the course do not affect the contract. The paid amount is the sum of confirmed payments minus
/// refunds, both applied by the payment workflow. The debt is the price minus the paid amount and is negative when
/// the student has overpaid.
/// </para>
/// <para>
/// Progress, price and dates change only while the contract is active. Payments and refunds are accepted in any
/// status, because a debt can be settled, or money returned, after the contract is closed.
/// </para>
/// </remarks>
public sealed class Contract : AuditableEntity
{
    /// <summary>The number of most recent practice grades the quality score is averaged over.</summary>
    public const int QualityWindowSize = 10;

    /// <summary>The lowest practice grade.</summary>
    public const decimal MinGrade = 1m;

    /// <summary>The highest practice grade.</summary>
    public const decimal MaxGrade = 5m;

    /// <summary>The longest practice lesson that can be recorded at once, in hours.</summary>
    public const decimal MaxLessonHours = 12m;

    private const int MaxNumberLength = 50;
    private const int MaxTerminationReasonLength = 1000;
    private const int GradeDecimals = 2;

    private readonly List<decimal> _recentGrades = [];

    /// <summary>Gets the contract number printed on the paper contract, unique within the school.</summary>
    public string Number { get; private set; } = null!;

    /// <summary>Gets the identifier of the student (a <see cref="User"/>).</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the identifier of the course.</summary>
    public Guid CourseId { get; private set; }

    /// <summary>Gets the licence category of the course, copied when the contract was drawn up.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the identifier of the branch, or <see langword="null"/> if the contract is not tied to one.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the first day of training.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Gets the planned last day of training.</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Gets the agreed price.</summary>
    public Money Price { get; private set; } = null!;

    /// <summary>Gets the sum of confirmed payments minus refunds, in the price currency.</summary>
    public Money PaidAmount { get; private set; } = null!;

    /// <summary>Gets the amount still owed; negative if the student has overpaid.</summary>
    public Money Debt => Price - PaidAmount;

    /// <summary>Gets the practical driving hours completed so far.</summary>
    public decimal CompletedPracticeHours { get; private set; }

    /// <summary>Gets the number of theory lessons the student attended.</summary>
    public int AttendedTheoryLessons { get; private set; }

    /// <summary>Gets the most recent practice grades, oldest first, at most <see cref="QualityWindowSize"/>.</summary>
    public IReadOnlyList<decimal> RecentGrades => _recentGrades.AsReadOnly();

    /// <summary>
    /// Gets the training quality score: the average of <see cref="RecentGrades"/> rounded to two decimals,
    /// or <see langword="null"/> before the first graded lesson.
    /// </summary>
    public decimal? QualityScore => _recentGrades.Count == 0
        ? null
        : decimal.Round(_recentGrades.Average(), GradeDecimals, MidpointRounding.AwayFromZero);

    /// <summary>Gets the contract status.</summary>
    public ContractStatus Status { get; private set; }

    /// <summary>Gets the day the contract was completed or terminated, or <see langword="null"/> while it is active.</summary>
    public DateOnly? ClosedOn { get; private set; }

    /// <summary>Gets the reason the contract was terminated, or <see langword="null"/> if it was not terminated.</summary>
    public string? TerminationReason { get; private set; }

    private Contract(
        Guid id, string number, Guid studentId, Course course, Guid? branchId, Money price, DateOnly startDate, DateOnly endDate)
        : base(id)
    {
        Number = number;
        StudentId = studentId;
        CourseId = course.Id;
        Category = course.Category;
        BranchId = branchId;
        Price = price;
        PaidAmount = Money.Zero(price.Currency);
        StartDate = startDate;
        EndDate = endDate;
        Status = ContractStatus.Active;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Contract()
    {
    }

    /// <summary>Draws up a new active contract and raises <see cref="ContractCreatedDomainEvent"/>.</summary>
    /// <param name="number">The contract number. Required; uniqueness is checked by the caller.</param>
    /// <param name="student">The student. Checking that the user holds the student role is up to the caller.</param>
    /// <param name="course">The course; its identifier and licence category are copied.</param>
    /// <param name="price">The agreed price, usually the course price; must be greater than zero.</param>
    /// <param name="branchId">The branch, if any.</param>
    /// <param name="startDate">The first day of training.</param>
    /// <param name="endDate">The planned last day of training; must be later than the start.</param>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <returns>The created contract, or the first validation failure encountered.</returns>
    public static Result<Contract> Create(
        string? number,
        User student,
        Course course,
        Money price,
        Guid? branchId,
        DateOnly startDate,
        DateOnly endDate,
        DateTime utcNow)
    {
        var numberResult = Guard.RequiredText(number, MaxNumberLength, DomainErrors.Contract.NumberEmpty, DomainErrors.Contract.NumberTooLong);
        if (numberResult.IsFailure)
            return Result.Failure<Contract>(numberResult.Error);

        if (!student.IsActive)
            return Result.Failure<Contract>(DomainErrors.Contract.StudentInactive);

        if (!course.IsActive)
            return Result.Failure<Contract>(DomainErrors.Contract.CourseInactive);

        if (!price.IsPositive)
            return Result.Failure<Contract>(DomainErrors.Contract.PriceNotPositive);

        if (branchId is { } branch && branch == Guid.Empty)
            return Result.Failure<Contract>(DomainErrors.Contract.BranchIdEmpty);

        if (endDate <= startDate)
            return Result.Failure<Contract>(DomainErrors.Contract.EndNotAfterStart);

        var contract = new Contract(Guid.CreateVersion7(), numberResult.Value, student.Id, course, branchId, price, startDate, endDate);
        contract.RaiseDomainEvent(new ContractCreatedDomainEvent(contract.Id, student.Id, course.Id, utcNow));

        return contract;
    }

    /// <summary>
    /// Changes the agreed price. The currency cannot change, because payments are recorded in it.
    /// A price below the paid amount is allowed and leaves an overpayment to refund.
    /// </summary>
    /// <returns>A failure if the contract is not active, the price is not positive, or its currency differs.</returns>
    public Result ChangePrice(Money price)
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        if (!price.HasSameCurrencyAs(Price))
            return Result.Failure(DomainErrors.Contract.CurrencyMismatch);

        if (!price.IsPositive)
            return Result.Failure(DomainErrors.Contract.PriceNotPositive);

        Price = price;

        return Result.Success();
    }

    /// <summary>Changes the training period.</summary>
    /// <returns>A failure if the contract is not active or the end is not later than the start.</returns>
    public Result Reschedule(DateOnly startDate, DateOnly endDate)
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        if (endDate <= startDate)
            return Result.Failure(DomainErrors.Contract.EndNotAfterStart);

        StartDate = startDate;
        EndDate = endDate;

        return Result.Success();
    }

    /// <summary>Adds a confirmed payment to the paid amount. Called by the payment workflow.</summary>
    /// <returns>A failure if the amount is not positive or is in another currency.</returns>
    public Result ApplyPayment(Money amount)
    {
        var amountResult = ValidateAmount(amount);
        if (amountResult.IsFailure)
            return amountResult;

        PaidAmount += amount;

        return Result.Success();
    }

    /// <summary>Subtracts a refund from the paid amount. Called by the payment workflow.</summary>
    /// <returns>A failure if the amount is not positive, is in another currency, or exceeds the paid amount.</returns>
    public Result ApplyRefund(Money amount)
    {
        var amountResult = ValidateAmount(amount);
        if (amountResult.IsFailure)
            return amountResult;

        if (amount > PaidAmount)
            return Result.Failure(DomainErrors.Contract.RefundExceedsPaidAmount);

        PaidAmount -= amount;

        return Result.Success();
    }

    /// <summary>Records a completed practical lesson: adds its hours and, if graded, its grade.</summary>
    /// <param name="hours">The actual length of the lesson, greater than 0 and at most <see cref="MaxLessonHours"/>, with at most two decimals.</param>
    /// <param name="grade">
    /// The lesson grade (the average of the skill grades), from <see cref="MinGrade"/> to <see cref="MaxGrade"/>
    /// with at most two decimals, or <see langword="null"/> if the lesson was not graded.
    /// </param>
    /// <returns>A failure if the contract is not active, or the hours or the grade are out of range.</returns>
    public Result RecordPracticeLesson(decimal hours, decimal? grade)
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        if (hours <= 0m || hours > MaxLessonHours || decimal.Round(hours, 2) != hours)
            return Result.Failure(DomainErrors.Contract.LessonHoursOutOfRange);

        if (grade is { } value && (value < MinGrade || value > MaxGrade || decimal.Round(value, GradeDecimals) != value))
            return Result.Failure(DomainErrors.Contract.GradeOutOfRange);

        CompletedPracticeHours += hours;

        if (grade is { } recorded)
        {
            _recentGrades.Add(recorded);
            if (_recentGrades.Count > QualityWindowSize)
                _recentGrades.RemoveAt(0);
        }

        return Result.Success();
    }

    /// <summary>Counts a theory lesson the student attended.</summary>
    /// <returns>A failure if the contract is not active.</returns>
    public Result RecordTheoryAttendance()
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        AttendedTheoryLessons++;

        return Result.Success();
    }

    /// <summary>Takes back a theory attendance that was recorded by mistake.</summary>
    /// <returns>A failure if the contract is not active or no attendance has been recorded.</returns>
    public Result RevokeTheoryAttendance()
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        if (AttendedTheoryLessons == 0)
            return Result.Failure(DomainErrors.Contract.NoTheoryAttendance);

        AttendedTheoryLessons--;

        return Result.Success();
    }

    /// <summary>Completes the contract after successful training and raises <see cref="ContractClosedDomainEvent"/>.</summary>
    /// <param name="completedOn">The day of completion; not earlier than the start.</param>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <returns>A failure if the contract is not active, the date is before the start, or a debt remains.</returns>
    public Result Complete(DateOnly completedOn, DateTime utcNow)
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        if (completedOn < StartDate)
            return Result.Failure(DomainErrors.Contract.ClosedBeforeStart);

        if (Debt.IsPositive)
            return Result.Failure(DomainErrors.Contract.OutstandingDebt);

        Close(ContractStatus.Completed, completedOn, null, utcNow);

        return Result.Success();
    }

    /// <summary>Terminates the contract early and raises <see cref="ContractClosedDomainEvent"/>.</summary>
    /// <param name="reason">Why the contract was terminated. Required.</param>
    /// <param name="terminatedOn">The day of termination; not earlier than the start.</param>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <returns>A failure if the contract is not active, the reason is invalid, or the date is before the start.</returns>
    public Result Terminate(string? reason, DateOnly terminatedOn, DateTime utcNow)
    {
        if (Status != ContractStatus.Active)
            return Result.Failure(DomainErrors.Contract.NotActive);

        var reasonResult = Guard.RequiredText(
            reason, MaxTerminationReasonLength, DomainErrors.Contract.TerminationReasonEmpty, DomainErrors.Contract.TerminationReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        if (terminatedOn < StartDate)
            return Result.Failure(DomainErrors.Contract.ClosedBeforeStart);

        Close(ContractStatus.Terminated, terminatedOn, reasonResult.Value, utcNow);

        return Result.Success();
    }

    private void Close(ContractStatus status, DateOnly closedOn, string? terminationReason, DateTime utcNow)
    {
        Status = status;
        ClosedOn = closedOn;
        TerminationReason = terminationReason;
        RaiseDomainEvent(new ContractClosedDomainEvent(Id, StudentId, status, utcNow));
    }

    private Result ValidateAmount(Money amount)
    {
        if (!amount.HasSameCurrencyAs(Price))
            return Result.Failure(DomainErrors.Contract.CurrencyMismatch);

        if (!amount.IsPositive)
            return Result.Failure(DomainErrors.Contract.AmountNotPositive);

        return Result.Success();
    }
}

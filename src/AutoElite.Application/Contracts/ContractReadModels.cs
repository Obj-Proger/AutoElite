namespace AutoElite.Application.Contracts;

/// <summary>The conditions of the contract list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Search">Text to find in the contract number or the student's name or email, case-insensitively.</param>
/// <param name="Status">Only contracts in this status.</param>
/// <param name="CourseId">Only contracts for this course.</param>
/// <param name="BranchId">Only contracts of this branch.</param>
/// <param name="StudentId">Only contracts of this student.</param>
/// <param name="HasDebt">Only contracts with, or only without, an amount still owed.</param>
public sealed record ContractListFilter(
    string? Search,
    ContractStatus? Status,
    Guid? CourseId,
    Guid? BranchId,
    Guid? StudentId,
    bool? HasDebt);

/// <summary>A row of the contract list.</summary>
/// <param name="Id">The identifier of the contract.</param>
/// <param name="Number">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="CourseId">The course.</param>
/// <param name="CourseName">The name of the course.</param>
/// <param name="Category">The licence category.</param>
/// <param name="Transmission">The gearbox the student trains on.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="Status">The status.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training.</param>
/// <param name="Currency">The ISO 4217 code of the contract currency.</param>
/// <param name="PriceAmount">The agreed price.</param>
/// <param name="PaidAmount">The confirmed payments minus refunds.</param>
/// <param name="DebtAmount">The amount still owed; negative for an overpayment.</param>
public sealed record ContractListItem(
    Guid Id,
    string Number,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseName,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId,
    string? BranchName,
    ContractStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    string Currency,
    decimal PriceAmount,
    decimal PaidAmount,
    decimal DebtAmount);

/// <summary>The card of a contract with the progress of the training (FR-4.2, FR-4.3).</summary>
/// <param name="Id">The identifier of the contract.</param>
/// <param name="Number">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="CourseId">The course.</param>
/// <param name="CourseName">The name of the course.</param>
/// <param name="Category">The licence category.</param>
/// <param name="Transmission">The gearbox the student trains on.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="Status">The status.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training.</param>
/// <param name="ClosedOn">The day the contract was completed or terminated, if it is closed.</param>
/// <param name="TerminationReason">The reason of the termination, if it was terminated.</param>
/// <param name="Currency">The ISO 4217 code of the contract currency.</param>
/// <param name="PriceAmount">The agreed price.</param>
/// <param name="PaidAmount">The confirmed payments minus refunds.</param>
/// <param name="DebtAmount">The amount still owed; negative for an overpayment.</param>
/// <param name="CompletedPracticeHours">The driving hours completed so far.</param>
/// <param name="CoursePracticeHours">The driving hours of the course.</param>
/// <param name="AttendedTheoryLessons">The theory lessons attended.</param>
/// <param name="QualityScore">The training quality score, if any lesson was graded.</param>
/// <param name="CreatedAtUtc">When the contract was drawn up.</param>
public sealed record ContractDetails(
    Guid Id,
    string Number,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseName,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId,
    string? BranchName,
    ContractStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? ClosedOn,
    string? TerminationReason,
    string Currency,
    decimal PriceAmount,
    decimal PaidAmount,
    decimal DebtAmount,
    decimal CompletedPracticeHours,
    int CoursePracticeHours,
    int AttendedTheoryLessons,
    decimal? QualityScore,
    DateTime CreatedAtUtc);

using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads contracts with the names of their student, course and branch, without tracking.</summary>
internal sealed class ContractQueries(AutoEliteDbContext dbContext) : IContractQueries
{
    /// <inheritdoc />
    public async Task<PagedList<ContractListItem>> GetListAsync(
        ContractListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Rows();
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
        {
            // The email column holds the address as text; the casts let SQL search it although it is a value object.
            rows = rows.Where(row =>
                EF.Functions.ILike(row.Number, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentLastName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentFirstName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentMiddleName!, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike((string)(object)row.StudentEmail, pattern, QueryExtensions.LikeEscape));
        }

        if (filter.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        if (filter.CourseId is { } courseId)
            rows = rows.Where(row => row.CourseId == courseId);

        if (filter.BranchId is { } branchId)
            rows = rows.Where(row => row.BranchId == branchId);

        if (filter.StudentId is { } studentId)
            rows = rows.Where(row => row.StudentId == studentId);

        if (filter.HasDebt is { } hasDebt)
            rows = rows.Where(row => (row.PriceAmount > row.PaidAmount) == hasDebt);

        var (found, totalCount) = await Latest(rows).ReadPageAsync(page, cancellationToken);

        return new PagedList<ContractListItem>([.. found.Select(ToListItem)], page, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractListItem>> GetOfStudentAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        var found = await Latest(Rows().Where(row => row.StudentId == studentId)).ToListAsync(cancellationToken);

        return [.. found.Select(ToListItem)];
    }

    /// <inheritdoc />
    public async Task<ContractDetails?> GetDetailsAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == contractId, cancellationToken);

        return row is null
            ? null
            : new ContractDetails(
                row.Id, row.Number, row.StudentId, StudentName(row), row.CourseId, row.CourseName, row.Category,
                row.Transmission, row.BranchId, row.BranchName, row.Status, row.StartDate, row.EndDate, row.ClosedOn,
                row.TerminationReason, row.Currency.Code, row.PriceAmount, row.PaidAmount, row.PriceAmount - row.PaidAmount,
                row.CompletedPracticeHours, row.CoursePracticeHours, row.AttendedTheoryLessons, row.QualityScore,
                row.CreatedAtUtc);
    }

    private static IQueryable<ContractRow> Latest(IQueryable<ContractRow> rows) =>
        rows.OrderByDescending(row => row.StartDate).ThenByDescending(row => row.Number);

    private static ContractListItem ToListItem(ContractRow row) =>
        new(
            row.Id, row.Number, row.StudentId, StudentName(row), row.CourseId, row.CourseName, row.Category,
            row.Transmission, row.BranchId, row.BranchName, row.Status, row.StartDate, row.EndDate, row.Currency.Code,
            row.PriceAmount, row.PaidAmount, row.PriceAmount - row.PaidAmount);

    private static string StudentName(ContractRow row) =>
        PersonNames.Format(row.StudentLastName, row.StudentFirstName, row.StudentMiddleName)!;

    private IQueryable<ContractRow> Rows() =>
        from contract in dbContext.Set<Contract>().AsNoTracking()
        join student in dbContext.Set<User>() on contract.StudentId equals student.Id
        join course in dbContext.Set<Course>() on contract.CourseId equals course.Id
        join branch in dbContext.Set<Branch>() on contract.BranchId equals (Guid?)branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        select new ContractRow
        {
            Id = contract.Id,
            Number = contract.Number,
            StudentId = contract.StudentId,
            StudentLastName = student.FullName.LastName,
            StudentFirstName = student.FullName.FirstName,
            StudentMiddleName = student.FullName.MiddleName,
            StudentEmail = student.Email,
            CourseId = contract.CourseId,
            CourseName = course.Name,
            CoursePracticeHours = course.PracticeHours,
            Category = contract.Category,
            Transmission = contract.Transmission,
            BranchId = contract.BranchId,
            BranchName = branch == null ? null : branch.Name,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            ClosedOn = contract.ClosedOn,
            TerminationReason = contract.TerminationReason,
            Currency = contract.Price.Currency,
            PriceAmount = contract.Price.Amount,
            PaidAmount = contract.PaidAmount.Amount,
            CompletedPracticeHours = contract.CompletedPracticeHours,
            AttendedTheoryLessons = contract.AttendedTheoryLessons,
            QualityScore = contract.QualityScore,
            CreatedAtUtc = contract.CreatedAtUtc
        };

    /// <summary>The columns of a contract that the read models are built from.</summary>
    private sealed class ContractRow
    {
        public Guid Id { get; init; }

        public string Number { get; init; } = null!;

        public Guid StudentId { get; init; }

        public string StudentLastName { get; init; } = null!;

        public string StudentFirstName { get; init; } = null!;

        public string? StudentMiddleName { get; init; }

        public Email StudentEmail { get; init; } = null!;

        public Guid CourseId { get; init; }

        public string CourseName { get; init; } = null!;

        public int CoursePracticeHours { get; init; }

        public LicenseCategory Category { get; init; }

        public Transmission Transmission { get; init; }

        public Guid? BranchId { get; init; }

        public string? BranchName { get; init; }

        public ContractStatus Status { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }

        public DateOnly? ClosedOn { get; init; }

        public string? TerminationReason { get; init; }

        public Currency Currency { get; init; } = null!;

        public decimal PriceAmount { get; init; }

        public decimal PaidAmount { get; init; }

        public decimal CompletedPracticeHours { get; init; }

        public int AttendedTheoryLessons { get; init; }

        public decimal? QualityScore { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}

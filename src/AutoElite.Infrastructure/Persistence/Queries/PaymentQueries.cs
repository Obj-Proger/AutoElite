using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Payments;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads payments with their contract, student and registrar, without tracking.</summary>
internal sealed class PaymentQueries(AutoEliteDbContext dbContext) : IPaymentQueries
{
    /// <inheritdoc />
    public async Task<ContractPayments?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Set<Contract>().AsNoTracking()
            .Where(found => found.Id == contractId)
            .Select(found => new { found.StudentId, found.BranchId })
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null)
            return null;

        var payments = await Rows()
            .Where(row => row.ContractId == contractId)
            .OrderBy(row => row.RegisteredAtUtc)
            .ThenBy(row => row.Id)
            .ToListAsync(cancellationToken);

        return new ContractPayments(contract.StudentId, contract.BranchId, [.. payments.Select(ToItem)]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var payments = await Latest(Rows().Where(row => row.StudentId == studentId)).ToListAsync(cancellationToken);

        return [.. payments.Select(ToItem)];
    }

    /// <inheritdoc />
    public async Task<PaymentReport> GetReportAsync(
        PaymentReportFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Matching(filter, scope);
        var (found, totalCount) = await Latest(rows).ReadPageAsync(page, cancellationToken);
        var totals = await rows
            .GroupBy(row => row.Currency)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(row => row.Amount), Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new PaymentReport(
            new PagedList<PaymentItem>([.. found.Select(ToItem)], page, totalCount),
            [
                .. totals
                    .Select(total => new CurrencyTotal(total.Currency.Code, total.Amount, total.Count))
                    .OrderBy(total => total.Currency, StringComparer.Ordinal)
            ]);
    }

    private static IQueryable<PaymentRow> Latest(IQueryable<PaymentRow> rows) =>
        rows.OrderByDescending(row => row.RegisteredAtUtc).ThenByDescending(row => row.Id);

    private static PaymentItem ToItem(PaymentRow row) =>
        new(
            row.Id, row.ContractId, row.ContractNumber, row.StudentId,
            PersonNames.Format(row.StudentLastName, row.StudentFirstName, row.StudentMiddleName)!, row.Amount,
            row.Currency.Code, row.Method, row.Purpose, row.ExternalReference, row.Status, row.RegisteredAtUtc,
            row.RegisteredByUserId,
            PersonNames.Format(row.RegistrarLastName, row.RegistrarFirstName, row.RegistrarMiddleName), row.ConfirmedAtUtc,
            row.RefundedAtUtc, row.CancelledAtUtc, row.Reason);

    /// <summary>The payments of the report: the filter applied and the scope enforced.</summary>
    private IQueryable<PaymentRow> Matching(PaymentReportFilter filter, DataScope scope)
    {
        var rows = Rows();
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (filter.From is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            rows = rows.Where(row => row.RegisteredAtUtc >= fromUtc);
        }

        if (filter.To is { } to)
        {
            var beforeUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            rows = rows.Where(row => row.RegisteredAtUtc < beforeUtc);
        }

        if (filter.Method is { } method)
            rows = rows.Where(row => row.Method == method);

        if (filter.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        if (filter.RegisteredByUserId is { } registrarId)
            rows = rows.Where(row => row.RegisteredByUserId == registrarId);

        if (filter.BranchId is { } branchId)
            rows = rows.Where(row => row.BranchId == branchId);

        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
        {
            rows = rows.Where(row =>
                EF.Functions.ILike(row.ContractNumber, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentLastName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentFirstName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(row.StudentMiddleName!, pattern, QueryExtensions.LikeEscape));
        }

        return rows;
    }

    private IQueryable<PaymentRow> Rows() =>
        from payment in dbContext.Set<Payment>().AsNoTracking()
        join contract in dbContext.Set<Contract>() on payment.ContractId equals contract.Id
        join student in dbContext.Set<User>() on payment.StudentId equals student.Id
        join registrar in dbContext.Set<User>() on payment.RegisteredByUserId equals (Guid?)registrar.Id into registrars
        from registrar in registrars.DefaultIfEmpty()
        select new PaymentRow
        {
            Id = payment.Id,
            ContractId = payment.ContractId,
            ContractNumber = contract.Number,
            BranchId = contract.BranchId,
            StudentId = payment.StudentId,
            StudentLastName = student.FullName.LastName,
            StudentFirstName = student.FullName.FirstName,
            StudentMiddleName = student.FullName.MiddleName,
            Amount = payment.Amount.Amount,
            Currency = payment.Amount.Currency,
            Method = payment.Method,
            Purpose = payment.Purpose,
            ExternalReference = payment.ExternalReference,
            Status = payment.Status,
            RegisteredAtUtc = payment.RegisteredAtUtc,
            RegisteredByUserId = payment.RegisteredByUserId,
            RegistrarLastName = registrar == null ? null : registrar.FullName.LastName,
            RegistrarFirstName = registrar == null ? null : registrar.FullName.FirstName,
            RegistrarMiddleName = registrar == null ? null : registrar.FullName.MiddleName,
            ConfirmedAtUtc = payment.ConfirmedAtUtc,
            RefundedAtUtc = payment.RefundedAtUtc,
            CancelledAtUtc = payment.CancelledAtUtc,
            Reason = payment.Reason
        };

    /// <summary>The columns of a payment that the read models are built from.</summary>
    private sealed class PaymentRow
    {
        public Guid Id { get; init; }

        public Guid ContractId { get; init; }

        public string ContractNumber { get; init; } = null!;

        public Guid? BranchId { get; init; }

        public Guid StudentId { get; init; }

        public string StudentLastName { get; init; } = null!;

        public string StudentFirstName { get; init; } = null!;

        public string? StudentMiddleName { get; init; }

        public decimal Amount { get; init; }

        public Currency Currency { get; init; } = null!;

        public PaymentMethod Method { get; init; }

        public string Purpose { get; init; } = null!;

        public string? ExternalReference { get; init; }

        public PaymentStatus Status { get; init; }

        public DateTime RegisteredAtUtc { get; init; }

        public Guid? RegisteredByUserId { get; init; }

        public string? RegistrarLastName { get; init; }

        public string? RegistrarFirstName { get; init; }

        public string? RegistrarMiddleName { get; init; }

        public DateTime? ConfirmedAtUtc { get; init; }

        public DateTime? RefundedAtUtc { get; init; }

        public DateTime? CancelledAtUtc { get; init; }

        public string? Reason { get; init; }
    }
}

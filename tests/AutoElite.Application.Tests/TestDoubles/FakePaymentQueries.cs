using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Payments;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded histories and records the report arguments; filtering and totals are the infrastructure's job.</summary>
internal sealed class FakePaymentQueries : IPaymentQueries
{
    private readonly Dictionary<Guid, ContractPayments> _contracts = [];

    public List<PaymentItem> StudentPayments { get; } = [];

    public (PaymentReportFilter Filter, DataScope Scope, PageRequest Page)? LastReportCall { get; private set; }

    public void Seed(Guid contractId, ContractPayments payments) => _contracts.Add(contractId, payments);

    public Task<ContractPayments?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_contracts.GetValueOrDefault(contractId));

    public Task<IReadOnlyList<PaymentItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentItem>>([.. StudentPayments.Where(payment => payment.StudentId == studentId)]);

    public Task<PaymentReport> GetReportAsync(
        PaymentReportFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastReportCall = (filter, scope, page);

        return Task.FromResult(new PaymentReport(PagedList<PaymentItem>.Empty(page), [new CurrencyTotal("RUB", 15000m, 1)]));
    }
}

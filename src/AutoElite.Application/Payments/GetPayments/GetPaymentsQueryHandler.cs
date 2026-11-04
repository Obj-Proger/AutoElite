using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Payments.GetPayments;

/// <summary>Reads the report within the caller's scope; a branch outside the scope gives an empty report.</summary>
internal sealed class GetPaymentsQueryHandler(IPaymentQueries paymentQueries, ICurrentUser currentUser)
    : IQueryHandler<GetPaymentsQuery, PaymentReport>
{
    /// <inheritdoc />
    public async Task<Result<PaymentReport>> Handle(GetPaymentsQuery query, CancellationToken cancellationToken)
    {
        var page = PageRequest.From(query);
        var scope = DataScope.For(currentUser);

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return new PaymentReport(PagedList<PaymentItem>.Empty(page), []);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new PaymentReportFilter(
            query.From, query.To, query.Method, query.Status, query.RegisteredByUserId, query.BranchId, search);

        return await paymentQueries.GetReportAsync(filter, scope, page, cancellationToken);
    }
}

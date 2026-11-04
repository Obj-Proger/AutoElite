namespace AutoElite.Application.Payments.GetMyPayments;

/// <summary>Reads the caller's payments.</summary>
internal sealed class GetMyPaymentsQueryHandler(IPaymentQueries paymentQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyPaymentsQuery, IReadOnlyList<PaymentItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentItem>>> Handle(GetMyPaymentsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await paymentQueries.GetOfStudentAsync(currentUser.GetRequiredUserId(), cancellationToken));
}

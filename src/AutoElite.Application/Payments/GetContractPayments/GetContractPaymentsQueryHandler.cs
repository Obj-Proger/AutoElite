namespace AutoElite.Application.Payments.GetContractPayments;

/// <summary>Reads the history and hides contracts the caller may not see as not found.</summary>
internal sealed class GetContractPaymentsQueryHandler(IPaymentQueries paymentQueries, ICurrentUser currentUser)
    : IQueryHandler<GetContractPaymentsQuery, IReadOnlyList<PaymentItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentItem>>> Handle(GetContractPaymentsQuery query, CancellationToken cancellationToken)
    {
        var history = await paymentQueries.GetOfContractAsync(query.ContractId, cancellationToken);
        if (history is null || !MayRead(history))
            return Result.Failure<IReadOnlyList<PaymentItem>>(ApplicationErrors.NotFound<Contract>());

        return Result.Success(history.Payments);
    }

    private bool MayRead(ContractPayments history)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(history.BranchId))
            return true;

        return history.StudentId == currentUser.UserId;
    }
}

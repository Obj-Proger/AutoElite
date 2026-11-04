namespace AutoElite.Application.Payments.GetContractPayments;

/// <summary>
/// Reads the payment history of a contract (FR-11.5). Staff see the contracts of their scope; a student sees only the
/// history of their own contracts.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.Student)]
public sealed record GetContractPaymentsQuery(Guid ContractId) : IQuery<IReadOnlyList<PaymentItem>>;

namespace AutoElite.Application.Payments.GetMyPayments;

/// <summary>Reads the payments of the signed-in student across all of their contracts, the latest first.</summary>
[RequireRoles(UserRole.Student)]
public sealed record GetMyPaymentsQuery : IQuery<IReadOnlyList<PaymentItem>>;

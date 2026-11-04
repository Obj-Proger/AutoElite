namespace AutoElite.Application.Payments.RegisterPayment;

/// <summary>
/// Records a payment towards a contract (FR-11.1), in the contract currency. A payment counts towards the debt only
/// once it is confirmed; money received at the desk can be confirmed at once.
/// </summary>
/// <param name="ContractId">The contract the payment is for.</param>
/// <param name="Amount">The amount in the contract currency, greater than zero.</param>
/// <param name="Method">How the payment was made.</param>
/// <param name="Purpose">What the payment is for, for example "Tuition, first instalment". Required.</param>
/// <param name="ExternalReference">The identifier in the bank or gateway, if any.</param>
/// <param name="ConfirmNow">Whether to confirm the payment in the same action, for money already received.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record RegisterPaymentCommand(
    Guid ContractId,
    decimal Amount,
    PaymentMethod Method,
    string? Purpose,
    string? ExternalReference = null,
    bool ConfirmNow = false) : ICommand<Guid>;

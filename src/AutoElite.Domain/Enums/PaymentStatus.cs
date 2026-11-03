namespace AutoElite.Domain.Enums;

/// <summary>
/// The status of a payment: <see cref="Registered"/> to <see cref="Confirmed"/> or <see cref="Cancelled"/>,
/// and <see cref="Confirmed"/> to <see cref="Refunded"/>. Only a confirmed payment counts towards the contract.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Recorded but not yet confirmed (for example, an online payment awaiting the gateway).</summary>
    Registered = 1,

    /// <summary>The money was received and counted towards the contract.</summary>
    Confirmed = 2,

    /// <summary>The money was returned and taken off the contract; final.</summary>
    Refunded = 3,

    /// <summary>Recorded by mistake or failed before confirmation; never counted; final.</summary>
    Cancelled = 4
}

namespace AutoElite.Domain.Enums;

/// <summary>How a payment was made.</summary>
public enum PaymentMethod
{
    /// <summary>Cash at the school's office.</summary>
    Cash = 1,

    /// <summary>A bank card at the school's terminal.</summary>
    BankCard = 2,

    /// <summary>A bank transfer to the school's account.</summary>
    BankTransfer = 3,

    /// <summary>An online card payment through the payment gateway.</summary>
    Online = 4
}

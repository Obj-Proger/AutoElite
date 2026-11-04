namespace AutoElite.Application.Common;

/// <summary>Creates optional contact value objects from request values.</summary>
internal static class ContactDetails
{
    /// <summary>Creates a phone number, treating an empty value as no phone.</summary>
    /// <param name="phone">The phone number from the request.</param>
    /// <returns>The phone number, <see langword="null"/> for an empty value, or the validation failure.</returns>
    public static Result<PhoneNumber?> CreateOptionalPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Success<PhoneNumber?>(null);

        var phoneResult = PhoneNumber.Create(phone);

        return phoneResult.IsSuccess
            ? Result.Success<PhoneNumber?>(phoneResult.Value)
            : Result.Failure<PhoneNumber?>(phoneResult.Error);
    }

    /// <summary>Creates an email address, treating an empty value as no address.</summary>
    /// <param name="email">The email address from the request.</param>
    /// <returns>The address, <see langword="null"/> for an empty value, or the validation failure.</returns>
    public static Result<Email?> CreateOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Success<Email?>(null);

        var emailResult = Email.Create(email);

        return emailResult.IsSuccess ? Result.Success<Email?>(emailResult.Value) : Result.Failure<Email?>(emailResult.Error);
    }
}

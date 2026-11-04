namespace AutoElite.Application.Users;

/// <summary>Creates the optional value objects of a person's details from request values.</summary>
internal static class PersonDetails
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
}

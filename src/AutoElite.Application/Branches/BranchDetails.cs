namespace AutoElite.Application.Branches;

/// <summary>Creates the value objects of a branch from command values.</summary>
internal static class BranchDetails
{
    /// <summary>Creates the address and the optional phone; an empty phone means none.</summary>
    /// <param name="city">The city.</param>
    /// <param name="street">The street and building.</param>
    /// <param name="phone">The contact phone number, if any.</param>
    public static Result<(Address Address, PhoneNumber? Phone)> Create(string? city, string? street, string? phone)
    {
        var addressResult = Address.Create(city, street);
        if (addressResult.IsFailure)
            return Result.Failure<(Address, PhoneNumber?)>(addressResult.Error);

        var phoneResult = ContactDetails.CreateOptionalPhone(phone);
        if (phoneResult.IsFailure)
            return Result.Failure<(Address, PhoneNumber?)>(phoneResult.Error);

        return Result.Success((addressResult.Value, phoneResult.Value));
    }
}

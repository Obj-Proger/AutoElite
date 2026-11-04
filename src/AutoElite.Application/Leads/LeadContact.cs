namespace AutoElite.Application.Leads;

/// <summary>The contact details of a lead, parsed from command values.</summary>
/// <param name="Phone">The phone number, if given.</param>
/// <param name="Email">The email address, if given.</param>
internal sealed record LeadContact(PhoneNumber? Phone, Email? Email)
{
    /// <summary>Parses the optional phone and email; empty values mean none.</summary>
    /// <param name="phone">The phone number from the command.</param>
    /// <param name="email">The email address from the command.</param>
    public static Result<LeadContact> Parse(string? phone, string? email)
    {
        var phoneResult = ContactDetails.CreateOptionalPhone(phone);
        if (phoneResult.IsFailure)
            return Result.Failure<LeadContact>(phoneResult.Error);

        var emailResult = ContactDetails.CreateOptionalEmail(email);
        if (emailResult.IsFailure)
            return Result.Failure<LeadContact>(emailResult.Error);

        return new LeadContact(phoneResult.Value, emailResult.Value);
    }
}

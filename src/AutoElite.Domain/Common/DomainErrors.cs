namespace AutoElite.Domain.Common;

/// <summary>
/// Central catalog of domain-specific <see cref="Error"/> instances, grouped by concept.
/// </summary>
public static class DomainErrors
{
    /// <summary>Errors related to <see cref="ValueObjects.Email"/> validation.</summary>
    public static class Email
    {
        public static readonly Error Empty = new("Email.Empty", "Email address cannot be empty.", ErrorType.Validation);
        public static readonly Error TooLong = new("Email.TooLong", "Email address exceeds the maximum allowed length.", ErrorType.Validation);
        public static readonly Error InvalidFormat = new("Email.InvalidFormat", "Email address is not in a valid format.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.PhoneNumber"/> validation.</summary>
    public static class PhoneNumber
    {
        public static readonly Error Empty = new("PhoneNumber.Empty", "Phone number cannot be empty.", ErrorType.Validation);
        public static readonly Error InvalidFormat = new("PhoneNumber.InvalidFormat", "Phone number must be in international E.164 format, e.g. +14155552671.", ErrorType.Validation);
    }
}
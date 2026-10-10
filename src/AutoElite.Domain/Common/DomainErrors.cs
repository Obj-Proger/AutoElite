namespace AutoElite.Domain.Common;

/// <summary>
/// Central catalog of domain-specific <see cref="Error"/> instances, grouped by concept.
/// </summary>
public static class DomainErrors
{
    /// <summary>Errors related to <see cref="ValueObjects.Email"/> validation.</summary>
    public static class Email
    {
        /// <summary>The email address is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("Email.Empty", "Email address cannot be empty.", ErrorType.Validation);

        /// <summary>The email address exceeds the maximum allowed length.</summary>
        public static readonly Error TooLong = new("Email.TooLong", "Email address exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The email address does not match the expected structure.</summary>
        public static readonly Error InvalidFormat = new("Email.InvalidFormat", "Email address is not in a valid format.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.PhoneNumber"/> validation.</summary>
    public static class PhoneNumber
    {
        /// <summary>The phone number is null, empty, or whitespace.</summary>
        public static readonly Error Empty = new("PhoneNumber.Empty", "Phone number cannot be empty.", ErrorType.Validation);

        /// <summary>The phone number is not in international E.164 format.</summary>
        public static readonly Error InvalidFormat = new("PhoneNumber.InvalidFormat", "Phone number must be in international E.164 format, e.g. +79991234567.", ErrorType.Validation);
    }

    /// <summary>Errors related to <see cref="ValueObjects.FullName"/> validation.</summary>
    public static class FullName
    {
        /// <summary>The last name is null, empty, or whitespace.</summary>
        public static readonly Error LastNameEmpty = new("FullName.LastNameEmpty", "Last name cannot be empty.", ErrorType.Validation);

        /// <summary>The last name exceeds the maximum allowed length.</summary>
        public static readonly Error LastNameTooLong = new("FullName.LastNameTooLong", "Last name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The first name is null, empty, or whitespace.</summary>
        public static readonly Error FirstNameEmpty = new("FullName.FirstNameEmpty", "First name cannot be empty.", ErrorType.Validation);

        /// <summary>The first name exceeds the maximum allowed length.</summary>
        public static readonly Error FirstNameTooLong = new("FullName.FirstNameTooLong", "First name exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The middle name exceeds the maximum allowed length.</summary>
        public static readonly Error MiddleNameTooLong = new("FullName.MiddleNameTooLong", "Middle name exceeds the maximum allowed length.", ErrorType.Validation);
    }

    /// <summary>Errors related to the <see cref="Entities.User"/> entity.</summary>
    public static class User
    {
        /// <summary>The branch identifier was provided but is an empty GUID.</summary>
        public static readonly Error BranchIdEmpty = new("User.BranchIdEmpty", "Branch ID cannot be an empty GUID.", ErrorType.Validation);

        /// <summary>The photo URL is null, empty, or whitespace.</summary>
        public static readonly Error PhotoUrlEmpty = new("User.PhotoUrlEmpty", "Photo URL cannot be empty.", ErrorType.Validation);

        /// <summary>The photo URL exceeds the maximum allowed length.</summary>
        public static readonly Error PhotoUrlTooLong = new("User.PhotoUrlTooLong", "Photo URL exceeds the maximum allowed length.", ErrorType.Validation);

        /// <summary>The user is already active, so activating it again is not a valid transition.</summary>
        public static readonly Error AlreadyActive = new("User.AlreadyActive", "User is already active.", ErrorType.Conflict);

        /// <summary>The user is already inactive, so deactivating it again is not a valid transition.</summary>
        public static readonly Error AlreadyInactive = new("User.AlreadyInactive", "User is already inactive.", ErrorType.Conflict);

        /// <summary>The operation modifies an inactive user, which is not allowed.</summary>
        public static readonly Error CannotModifyInactive = new("User.CannotModifyInactive", "An inactive user cannot be modified. Reactivate the user first.", ErrorType.Conflict);
    }
}
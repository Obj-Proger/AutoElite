namespace AutoElite.Application.Common;

/// <summary>
/// Catalog of errors that originate in the application layer, as opposed to
/// <see cref="DomainErrors"/>, which are raised by domain rules.
/// </summary>
public static class ApplicationErrors
{
    /// <summary>The code of the error returned when request validation fails.</summary>
    public const string ValidationFailedCode = "Validation.Failed";

    /// <summary>The code assigned to a field error when the validation rule did not specify one.</summary>
    public const string InvalidFieldCode = "Validation.Invalid";

    /// <summary>Creates the error returned when a request fails validation, carrying every failed field.</summary>
    /// <param name="fieldErrors">The failures of the individual fields.</param>
    public static Error ValidationFailed(IReadOnlyList<FieldError> fieldErrors) =>
        Error.Validation(ValidationFailedCode, "One or more validation errors occurred.", fieldErrors);

    /// <summary>
    /// Creates the error returned when an entity does not exist or is outside the caller's data scope,
    /// so that callers cannot learn about records they may not see. The code is <c>{Entity}.NotFound</c>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the missing entity.</typeparam>
    public static Error NotFound<TEntity>()
        where TEntity : BaseEntity =>
        Error.NotFound($"{typeof(TEntity).Name}.NotFound", $"{typeof(TEntity).Name} was not found.");

    /// <summary>Errors of user accounts and invitations.</summary>
    public static class Account
    {
        /// <summary>A new user is given no roles.</summary>
        public static readonly Error RolesEmpty = Error.Validation("Account.RolesEmpty", "At least one role is required.");

        /// <summary>A role is listed more than once.</summary>
        public static readonly Error DuplicateRoles = Error.Validation("Account.DuplicateRoles", "Each role can be listed only once.");

        /// <summary>A manager tries to invite a user with a role other than student.</summary>
        public static readonly Error ManagerInvitesOnlyStudents = Error.Forbidden(
            "Account.ManagerInvitesOnlyStudents", "Managers can invite only students.");

        /// <summary>The user would be tied to an inactive branch.</summary>
        public static readonly Error BranchInactive = Error.Conflict(
            "Account.BranchInactive", "Users cannot be assigned to an inactive branch.");

        /// <summary>Another user already has the email address.</summary>
        public static readonly Error EmailTaken = Error.Conflict(
            "Account.EmailTaken", "A user with this email address already exists.");

        /// <summary>The user has already accepted the invitation and set a password.</summary>
        public static readonly Error AlreadyActivated = Error.Conflict(
            "Account.AlreadyActivated", "The user has already accepted the invitation.");
    }

    /// <summary>Errors of the page number and size of paged queries.</summary>
    public static class Paging
    {
        /// <summary>The page number is below 1 or above <see cref="Pagination.PageRequest.MaxPage"/>.</summary>
        public static readonly Error PageOutOfRange = Error.Validation(
            "Paging.PageOutOfRange", "Page must be between 1 and 100000.");

        /// <summary>The page size is below 1 or above <see cref="Pagination.PageRequest.MaxPageSize"/>.</summary>
        public static readonly Error PageSizeOutOfRange = Error.Validation(
            "Paging.PageSizeOutOfRange", "Page size must be between 1 and 100.");
    }

    /// <summary>Errors of saving changes.</summary>
    public static class Concurrency
    {
        /// <summary>Another request changed a record after this request loaded it.</summary>
        public static readonly Error Conflict = Error.Conflict(
            "Concurrency.Conflict", "The data was changed by someone else. Reload it and try again.");
    }

    /// <summary>Errors of authentication and authorization.</summary>
    public static class Auth
    {
        /// <summary>The request requires a signed-in user, but the caller is anonymous.</summary>
        public static readonly Error Unauthenticated = Error.Unauthorized("Auth.Unauthenticated", "You need to sign in.");

        /// <summary>The signed-in user does not have a role that the request requires.</summary>
        public static readonly Error Forbidden = Error.Forbidden("Auth.Forbidden", "You do not have permission to perform this action.");
    }
}

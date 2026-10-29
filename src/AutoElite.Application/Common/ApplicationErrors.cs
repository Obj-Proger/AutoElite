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
}

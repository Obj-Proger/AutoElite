namespace AutoElite.Domain.Common;

/// <summary>Describes the failure of a single input field, as part of a validation <see cref="Error"/>.</summary>
/// <param name="Field">The name of the field that failed validation, as it appears in the request.</param>
/// <param name="Code">
/// Machine-readable identifier of the failure, following the convention <c>Concept.ErrorName</c>.
/// Clients use it to choose a localized message.
/// </param>
/// <param name="Message">Human-readable description of the failure in English, intended for developers.</param>
public sealed record FieldError(string Field, string Code, string Message);

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Formats the names of people for read models as <see cref="FullName.ToString"/> does: "last first middle". Queries
/// read the parts as columns, since a person joined optionally may be missing.
/// </summary>
internal static class PersonNames
{
    /// <summary>Formats a full name, or returns <see langword="null"/> when the person is missing.</summary>
    /// <param name="lastName">The last name, <see langword="null"/> when the person is missing.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The middle name, if any.</param>
    public static string? Format(string? lastName, string? firstName, string? middleName) =>
        lastName is null ? null : middleName is null ? $"{lastName} {firstName}" : $"{lastName} {firstName} {middleName}";
}

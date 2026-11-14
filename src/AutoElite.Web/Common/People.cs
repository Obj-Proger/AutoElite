namespace AutoElite.Web.Common;

/// <summary>How the site writes the names of people.</summary>
internal static class People
{
    /// <summary>Gives the full name, last name first, as the lists and cards of the school show it.</summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The middle name, if any.</param>
    /// <returns>The full name.</returns>
    public static string FullName(string? lastName, string? firstName, string? middleName) =>
        string.Join(' ', new[] { lastName, firstName, middleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
}

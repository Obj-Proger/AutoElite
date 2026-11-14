namespace AutoElite.API.DemoData;

/// <summary>
/// The demo data of development, from the optional <see cref="SectionName"/> section: a branch with its staff,
/// students, vehicles, a course with a group, contracts, a schedule for the coming weeks and questions for practice
/// tests, so that every page of the website and of the application has something to show.
/// </summary>
internal sealed class DemoDataOptions
{
    /// <summary>The configuration section of the options.</summary>
    public const string SectionName = "DemoData";

    /// <summary>Gets or sets a value indicating whether the demo data is created; only in development.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the password of every demo user, kept in the user secrets, never in a file of the repository; it
    /// follows the password policy of the school.
    /// </summary>
    public string? Password { get; set; }
}

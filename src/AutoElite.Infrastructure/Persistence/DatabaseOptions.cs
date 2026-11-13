namespace AutoElite.Infrastructure.Persistence;

/// <summary>What the host does with the database when it starts, from the optional <see cref="SectionName"/> section.</summary>
public sealed class DatabaseOptions
{
    /// <summary>The configuration section of the options.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets a value indicating whether the host applies the pending migrations when it starts; on by default,
    /// off where the schema is updated otherwise, for example by a migration bundle in a deployment pipeline.
    /// </summary>
    public bool MigrateOnStartup { get; set; } = true;
}

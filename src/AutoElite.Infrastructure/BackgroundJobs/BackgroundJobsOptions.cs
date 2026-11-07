namespace AutoElite.Infrastructure.BackgroundJobs;

/// <summary>How the scheduled jobs run, from the optional <see cref="SectionName"/> configuration section.</summary>
internal sealed class BackgroundJobsOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>The database schema of Hangfire's own tables, apart from the school's.</summary>
    public const string SchemaName = "hangfire";

    /// <summary>Gets or sets the jobs one instance of the host runs at once.</summary>
    public int WorkerCount { get; set; } = 2;

    /// <summary>
    /// Gets or sets how long delivered outbox messages and sent emails are kept, for investigating what happened,
    /// before the daily clean-up removes them.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (WorkerCount < 1)
            yield return $"{SectionName}:WorkerCount must be at least 1.";

        if (RetentionPeriod < TimeSpan.FromDays(1))
            yield return $"{SectionName}:RetentionPeriod must be at least one day.";
    }
}

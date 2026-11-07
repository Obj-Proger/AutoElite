using AutoElite.Infrastructure.Common;

namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// How the outbox processor delivers domain events, from the optional <see cref="SectionName"/> configuration
/// section; the defaults suit a school.
/// </summary>
internal sealed class OutboxOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Outbox";

    /// <summary>Gets or sets how long the processor waits after finding no message to deliver.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets or sets the most messages delivered in one run before the processor looks again at once.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Gets or sets the deliveries of a message before it is given up as poisonous.</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Gets or sets the pause after the first failure; every further failure doubles it.</summary>
    public TimeSpan FirstRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the longest pause between deliveries of a failing message.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Gets the pause after the given number of failed deliveries.</summary>
    /// <param name="attempts">The failed deliveries so far, the latest included; at least 1.</param>
    public TimeSpan RetryDelayAfter(int attempts) => RetryDelays.After(attempts, FirstRetryDelay, MaxRetryDelay);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (PollingInterval <= TimeSpan.Zero)
            yield return $"{SectionName}:PollingInterval must be positive.";

        if (BatchSize < 1)
            yield return $"{SectionName}:BatchSize must be at least 1.";

        if (MaxAttempts < 1)
            yield return $"{SectionName}:MaxAttempts must be at least 1.";

        if (FirstRetryDelay <= TimeSpan.Zero)
            yield return $"{SectionName}:FirstRetryDelay must be positive.";

        if (MaxRetryDelay < FirstRetryDelay)
            yield return $"{SectionName}:MaxRetryDelay must not be shorter than {SectionName}:FirstRetryDelay.";
    }
}

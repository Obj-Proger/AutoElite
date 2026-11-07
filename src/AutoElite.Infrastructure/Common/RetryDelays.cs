namespace AutoElite.Infrastructure.Common;

/// <summary>The pause before retrying work that failed: doubled with every failure, up to a longest pause.</summary>
internal static class RetryDelays
{
    /// <summary>Gets the pause after the given number of failures.</summary>
    /// <param name="failures">The failures so far, the latest included; at least 1.</param>
    /// <param name="first">The pause after the first failure.</param>
    /// <param name="longest">The longest pause.</param>
    public static TimeSpan After(int failures, TimeSpan first, TimeSpan longest)
    {
        var doublings = Math.Clamp(failures - 1, 0, 30);
        var delay = first * Math.Pow(2, doublings);

        return delay < longest ? delay : longest;
    }
}

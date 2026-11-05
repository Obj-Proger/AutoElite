namespace AutoElite.Infrastructure.Tests.TestDoubles;

/// <summary>A clock that always shows the same time.</summary>
internal sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets a clock at 1 March 2026, 08:00 UTC.</summary>
    public static FixedClock Default { get; } = new(new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero));

    /// <summary>Gets the time the clock shows, in UTC.</summary>
    public DateTime UtcNow => utcNow.UtcDateTime;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => utcNow;
}

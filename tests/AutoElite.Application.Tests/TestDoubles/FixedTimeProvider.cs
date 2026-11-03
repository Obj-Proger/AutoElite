namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>A clock that always returns the same instant, so tests do not depend on the real time.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

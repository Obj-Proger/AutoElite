using System.Globalization;

namespace AutoElite.Domain.ValueObjects;

/// <summary>
/// A period of time between two UTC instants. The start is included and the end is excluded, so a lesson
/// from 10:00 to 11:00 and one from 11:00 to 12:00 do not overlap.
/// </summary>
public sealed class TimeInterval : ValueObject
{
    /// <summary>Gets the UTC instant at which the interval starts (inclusive).</summary>
    public DateTime StartUtc { get; }

    /// <summary>Gets the UTC instant at which the interval ends (exclusive).</summary>
    public DateTime EndUtc { get; }

    private TimeInterval(DateTime startUtc, DateTime endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    /// <summary>Gets the length of the interval.</summary>
    public TimeSpan Duration => EndUtc - StartUtc;

    /// <summary>Validates and creates a time interval.</summary>
    /// <returns>A failure if either instant is not in UTC, or the end is not later than the start.</returns>
    public static Result<TimeInterval> Create(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            return Result.Failure<TimeInterval>(DomainErrors.TimeInterval.NotUtc);

        if (endUtc <= startUtc)
            return Result.Failure<TimeInterval>(DomainErrors.TimeInterval.EndNotAfterStart);

        return new TimeInterval(startUtc, endUtc);
    }

    /// <summary>Determines whether this interval shares any moment with <paramref name="other"/>.</summary>
    public bool Overlaps(TimeInterval other) => StartUtc < other.EndUtc && other.StartUtc < EndUtc;

    /// <summary>Determines whether the UTC instant falls within the interval.</summary>
    public bool Contains(DateTime instantUtc) => instantUtc >= StartUtc && instantUtc < EndUtc;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartUtc;
        yield return EndUtc;
    }

    /// <summary>Formats the interval as two ISO 8601 instants, for example <c>2026-01-01T10:00:00Z/2026-01-01T11:00:00Z</c>.</summary>
    public override string ToString() =>
        $"{StartUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}/{EndUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}";
}

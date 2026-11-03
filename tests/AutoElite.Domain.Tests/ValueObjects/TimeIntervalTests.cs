namespace AutoElite.Domain.Tests.ValueObjects;

public class TimeIntervalTests
{
    private static DateTime At(int hour, int minute = 0) => new(2026, 3, 10, hour, minute, 0, DateTimeKind.Utc);

    private static TimeInterval Interval(int startHour, int endHour) => TimeInterval.Create(At(startHour), At(endHour)).Value;

    [Fact]
    public void Create_EndAfterStart_ReturnsInterval()
    {
        var result = TimeInterval.Create(At(10), At(11, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.StartUtc.Should().Be(At(10));
        result.Value.EndUtc.Should().Be(At(11, 30));
        result.Value.Duration.Should().Be(TimeSpan.FromMinutes(90));
    }

    [Theory]
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Unspecified)]
    public void Create_NonUtcInstant_ReturnsNotUtcError(DateTimeKind startKind, DateTimeKind endKind)
    {
        var start = DateTime.SpecifyKind(At(10), startKind);
        var end = DateTime.SpecifyKind(At(11), endKind);

        var result = TimeInterval.Create(start, end);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TimeInterval.NotUtc);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    public void Create_EndNotAfterStart_ReturnsEndNotAfterStartError(int startHour, int endHour)
    {
        var result = TimeInterval.Create(At(startHour), At(endHour));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TimeInterval.EndNotAfterStart);
    }

    [Theory]
    [InlineData(10, 12, 11, 13, true)]
    [InlineData(10, 14, 11, 12, true)]
    [InlineData(10, 12, 10, 12, true)]
    [InlineData(10, 11, 11, 12, false)]
    [InlineData(10, 11, 12, 13, false)]
    public void Overlaps_ReturnsWhetherIntervalsShareAMoment(int firstStart, int firstEnd, int secondStart, int secondEnd, bool expected)
    {
        var first = Interval(firstStart, firstEnd);
        var second = Interval(secondStart, secondEnd);

        first.Overlaps(second).Should().Be(expected);
        second.Overlaps(first).Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 0, true)]
    [InlineData(10, 59, true)]
    [InlineData(11, 0, false)]
    [InlineData(9, 59, false)]
    public void Contains_IncludesStartAndExcludesEnd(int hour, int minute, bool expected)
    {
        var interval = Interval(10, 11);

        interval.Contains(At(hour, minute)).Should().Be(expected);
    }

    [Fact]
    public void Equals_SameStartAndEnd_AreEqual()
    {
        Interval(10, 11).Should().Be(Interval(10, 11));
    }

    [Fact]
    public void ToString_FormatsAsIsoInterval()
    {
        Interval(10, 11).ToString().Should().Be("2026-03-10T10:00:00Z/2026-03-10T11:00:00Z");
    }
}

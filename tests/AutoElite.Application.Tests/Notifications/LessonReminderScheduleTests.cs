using AutoElite.Application.Notifications;

namespace AutoElite.Application.Tests.Notifications;

public class LessonReminderScheduleTests
{
    private static readonly DateTime _start = new(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan[] _leadTimes = [TimeSpan.FromHours(24), TimeSpan.FromHours(2)];

    private static TimeSpan? DueAt(TimeSpan beforeStart) =>
        LessonReminderSchedule.DueLeadTime(_leadTimes, _start, _start - beforeStart);

    [Fact]
    public void DueLeadTime_BeforeTheFirstMoment_IsNone()
    {
        DueAt(TimeSpan.FromHours(30)).Should().BeNull();
    }

    [Fact]
    public void DueLeadTime_JustAfterTheMomentOfADayAhead_IsThatReminder()
    {
        DueAt(TimeSpan.FromHours(23.5)).Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void DueLeadTime_BothMomentsPassed_IsOnlyTheClosestToTheLesson()
    {
        DueAt(TimeSpan.FromHours(1.5)).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void DueLeadTime_MomentPassedLongerAgoThanTheCatchUpWindow_IsNone()
    {
        DueAt(TimeSpan.FromHours(3)).Should().BeNull("the day-ahead moment passed 21 hours ago and the two-hour one has not come");
    }

    [Theory]
    [InlineData(59, true)]
    [InlineData(61, false)]
    public void DueLeadTime_MissedReminder_IsSentOnlyWithinTheCatchUpWindow(int minutesLate, bool due)
    {
        var result = DueAt(TimeSpan.FromHours(2) - TimeSpan.FromMinutes(minutesLate));

        (result is not null).Should().Be(due);
    }

    [Fact]
    public void DueLeadTime_LessonStarted_IsNone()
    {
        LessonReminderSchedule.DueLeadTime([TimeSpan.FromMinutes(15)], _start, _start).Should().BeNull();
    }
}

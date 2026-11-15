namespace AutoElite.Mobile.Pages;

/// <summary>The kind of a lesson, which gives its colour in the schedule.</summary>
public enum ScheduleCategory
{
    /// <summary>A theory lesson.</summary>
    Theory,

    /// <summary>A booked driving lesson.</summary>
    Driving,

    /// <summary>A free practice slot.</summary>
    Free,

    /// <summary>A cancelled lesson or one the student did not come to.</summary>
    Inactive,

    /// <summary>A school exam.</summary>
    Exam
}

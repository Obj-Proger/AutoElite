namespace AutoElite.Domain.Enums;

/// <summary>The status of a booking of a driving slot. Every status except <see cref="Active"/> is final.</summary>
public enum DrivingBookingStatus
{
    /// <summary>The student holds the slot.</summary>
    Active = 1,

    /// <summary>Cancelled by the student, the instructor or the school before the lesson.</summary>
    Cancelled = 2,

    /// <summary>The lesson took place.</summary>
    Completed = 3,

    /// <summary>The student did not show up.</summary>
    NoShow = 4
}

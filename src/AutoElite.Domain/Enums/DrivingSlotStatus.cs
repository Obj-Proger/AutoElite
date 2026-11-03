namespace AutoElite.Domain.Enums;

/// <summary>
/// The status of a driving slot: <see cref="Available"/> and <see cref="Booked"/> alternate while bookings are made
/// and cancelled; <see cref="Completed"/> and <see cref="Withdrawn"/> are final.
/// </summary>
public enum DrivingSlotStatus
{
    /// <summary>Open for booking.</summary>
    Available = 1,

    /// <summary>A student holds an active booking.</summary>
    Booked = 2,

    /// <summary>The lesson took place or the student did not show up.</summary>
    Completed = 3,

    /// <summary>The instructor withdrew the slot.</summary>
    Withdrawn = 4
}

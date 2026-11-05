namespace AutoElite.Domain.Enums;

/// <summary>What a notification is about (FR-10.1); clients choose an icon and grouping by it.</summary>
public enum NotificationType
{
    /// <summary>A new lead arrived.</summary>
    NewLead = 1,

    /// <summary>A lead was assigned to the recipient.</summary>
    LeadAssigned = 2,

    /// <summary>A student booked a driving lesson.</summary>
    DrivingLessonBooked = 3,

    /// <summary>A driving booking was cancelled.</summary>
    DrivingBookingCancelled = 4,

    /// <summary>The recipient is invited to rate a driving lesson.</summary>
    RateDrivingLesson = 5,

    /// <summary>A driving lesson received a low rating.</summary>
    LowDrivingLessonRating = 6,

    /// <summary>A theory lesson was cancelled or moved.</summary>
    TheoryLessonChanged = 7,

    /// <summary>A reminder of an upcoming lesson.</summary>
    LessonReminder = 8,

    /// <summary>An exam was scheduled, moved or cancelled.</summary>
    ExamChanged = 9,

    /// <summary>An exam result was recorded or corrected.</summary>
    ExamResult = 10,

    /// <summary>A payment was confirmed.</summary>
    PaymentConfirmed = 11,

    /// <summary>A payment was refunded.</summary>
    PaymentRefunded = 12,

    /// <summary>A new chat message.</summary>
    NewMessage = 13,

    /// <summary>A task was assigned to the recipient.</summary>
    TaskAssigned = 14,

    /// <summary>A task of the recipient is overdue.</summary>
    TaskOverdue = 15,

    /// <summary>The recipient joined a study group.</summary>
    EnrolledInGroup = 16,

    /// <summary>A reminder of an upcoming exam.</summary>
    ExamReminder = 17,

    /// <summary>Anything else.</summary>
    Other = 99
}

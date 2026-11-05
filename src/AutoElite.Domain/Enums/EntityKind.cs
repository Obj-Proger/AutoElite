namespace AutoElite.Domain.Enums;

/// <summary>The kind of object an <see cref="ValueObjects.EntityReference"/> points to.</summary>
public enum EntityKind
{
    /// <summary>A user account.</summary>
    User = 1,

    /// <summary>A branch.</summary>
    Branch = 2,

    /// <summary>A lead.</summary>
    Lead = 3,

    /// <summary>A course.</summary>
    Course = 4,

    /// <summary>A topic of a course.</summary>
    CourseTopic = 5,

    /// <summary>A contract.</summary>
    Contract = 6,

    /// <summary>A study group.</summary>
    StudyGroup = 7,

    /// <summary>A payment.</summary>
    Payment = 8,

    /// <summary>A theory lesson.</summary>
    TheoryLesson = 9,

    /// <summary>A driving slot and its booking.</summary>
    DrivingSlot = 10,

    /// <summary>A vehicle.</summary>
    Vehicle = 11,

    /// <summary>A test attempt.</summary>
    TestSession = 12,

    /// <summary>An exam.</summary>
    Exam = 13,

    /// <summary>A chat.</summary>
    Chat = 14,

    /// <summary>A chat message.</summary>
    ChatMessage = 15,

    /// <summary>A staff task.</summary>
    StaffTask = 16,

    /// <summary>A question of the test bank.</summary>
    Question = 17
}

namespace AutoElite.Domain.Enums;

/// <summary>
/// The set of roles a user account can be assigned within the driving school.
/// A user may hold multiple roles simultaneously — for example, the same
/// person can be both a <see cref="DrivingInstructor"/> and a <see cref="TheoryInstructor"/>.
/// </summary>
public enum UserRole
{
    /// <summary>Full system access: user and role management, branches, the course catalog, the vehicle fleet, the question bank, content import, and settings.</summary>
    Administrator = 1,

    /// <summary>Works with clients: CRM and leads, contracts, forming study groups, accepting and refunding payments, staff tasks, and reporting.</summary>
    Manager = 2,

    /// <summary>Manages practical training: driving slots, routes, student skill assessments, and recording practical exam results.</summary>
    DrivingInstructor = 3,

    /// <summary>Manages theoretical training: class schedules, attendance, course materials, the question bank, test templates, and assigning and administering exams.</summary>
    TheoryInstructor = 4,

    /// <summary>Self-service access: viewing the schedule, booking driving lessons, taking tests, viewing results and payment history, messaging, and documents.</summary>
    Student = 5
}

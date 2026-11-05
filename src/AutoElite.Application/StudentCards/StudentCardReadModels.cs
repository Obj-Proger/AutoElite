using AutoElite.Application.Contracts;
using AutoElite.Application.Exams;
using AutoElite.Application.TestSessions;

namespace AutoElite.Application.StudentCards;

/// <summary>The summary card of a student (FR-4.7): who they are and how each of their contracts goes.</summary>
/// <param name="StudentId">The student.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="PhotoDocumentId">The document with the profile photo, if any.</param>
/// <param name="BranchId">The branch the student is tied to, if any.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="Contracts">The contracts, the newest first.</param>
public sealed record StudentCard(
    Guid StudentId,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Email,
    string? Phone,
    Guid? PhotoDocumentId,
    Guid? BranchId,
    bool IsActive,
    IReadOnlyList<StudentCardContract> Contracts);

/// <summary>One contract on the student card.</summary>
/// <param name="Contract">The contract with payments, debt, practice hours and the quality score.</param>
/// <param name="Group">The study group the contract is currently enrolled in, if any.</param>
/// <param name="Driving">The driving lessons of the contract.</param>
/// <param name="Tests">The practice tests of the contract.</param>
/// <param name="Exams">The exams of the contract's groups with the student's results.</param>
public sealed record StudentCardContract(
    ContractDetails Contract,
    StudentCardGroup? Group,
    StudentCardDriving Driving,
    StudentCardTests Tests,
    IReadOnlyList<StudentExamItem> Exams);

/// <summary>The current study group of a contract and the student's attendance in it.</summary>
/// <param name="GroupId">The group.</param>
/// <param name="GroupName">The name of the group.</param>
/// <param name="LessonsHeld">The theory lessons of the group held so far.</param>
/// <param name="LessonsAttended">The lessons the student attended.</param>
public sealed record StudentCardGroup(Guid GroupId, string GroupName, int LessonsHeld, int LessonsAttended)
{
    /// <summary>Gets the lessons the student missed.</summary>
    public int LessonsMissed => LessonsHeld - LessonsAttended;
}

/// <summary>The driving lessons of a contract.</summary>
/// <param name="Completed">The completed lessons.</param>
/// <param name="NoShows">The lessons the student did not come to.</param>
/// <param name="Upcoming">The booked lessons still to come.</param>
/// <param name="AverageRating">The student's average rating of the lessons, if they rated any.</param>
public sealed record StudentCardDriving(int Completed, int NoShows, int Upcoming, decimal? AverageRating);

/// <summary>The practice tests of a contract; attempts of exams are among the exams.</summary>
/// <param name="Attempts">The attempts made.</param>
/// <param name="Passed">The attempts passed.</param>
/// <param name="Failed">The attempts failed.</param>
/// <param name="LastAttempt">The latest attempt, if any; its counts are hidden while it is in progress.</param>
public sealed record StudentCardTests(int Attempts, int Passed, int Failed, TestSessionItem? LastAttempt);

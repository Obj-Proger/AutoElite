using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>The lessons of a student, each list with where it came from.</summary>
/// <param name="Driving">The driving lessons, cancelled ones included.</param>
/// <param name="Theory">The theory lessons of the student's groups, with their attendance.</param>
public sealed record StudentLessons(
    Cached<IReadOnlyList<StudentDrivingLessonItem>> Driving, Cached<IReadOnlyList<StudentTheoryLessonItem>> Theory);

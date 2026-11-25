namespace AutoElite.Mobile.Schedule;

/// <summary>A student of a driving instructor, as their lessons of the last weeks and the next two show them.</summary>
/// <param name="StudentId">The student.</param>
/// <param name="Name">The full name of the student.</param>
/// <param name="LessonsDone">How many of those lessons were completed.</param>
/// <param name="Next">When the next lesson starts, if one is booked.</param>
/// <param name="Last">When the last lesson that started was, if there was one.</param>
public sealed record DrivingStudent(Guid StudentId, string Name, int LessonsDone, DateTimeOffset? Next, DateTimeOffset? Last);

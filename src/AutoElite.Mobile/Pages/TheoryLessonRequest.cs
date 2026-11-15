namespace AutoElite.Mobile.Pages;

/// <summary>
/// A theory lesson to plan, passed to the page that plans it: a new one starting at a time chosen on the schedule, or a
/// scheduled one to move.
/// </summary>
/// <param name="Start">The start, in the time zone of the device.</param>
/// <param name="End">The end of a lesson to move, or <see langword="null"/> for a new one.</param>
/// <param name="LessonId">The lesson to move, or <see langword="null"/> for a new one.</param>
/// <param name="Title">The topic and the group of the lesson to move, or <see langword="null"/>.</param>
public sealed record TheoryLessonRequest(DateTimeOffset Start, DateTimeOffset? End = null, Guid? LessonId = null, string? Title = null);

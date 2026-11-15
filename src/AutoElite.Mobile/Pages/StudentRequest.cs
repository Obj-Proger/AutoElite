namespace AutoElite.Mobile.Pages;

/// <summary>The student whose progress an instructor opens, passed to the page that shows it.</summary>
/// <param name="StudentId">The student.</param>
/// <param name="Name">The full name, shown while the progress is read.</param>
public sealed record StudentRequest(Guid StudentId, string Name);

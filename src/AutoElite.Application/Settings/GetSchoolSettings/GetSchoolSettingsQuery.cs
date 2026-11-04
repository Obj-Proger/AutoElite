namespace AutoElite.Application.Settings.GetSchoolSettings;

/// <summary>
/// Reads the school settings. Any signed-in user may read them, so that clients can show, for example, until when
/// a student may cancel a lesson.
/// </summary>
public sealed record GetSchoolSettingsQuery : IQuery<SchoolSettingsDetails>;

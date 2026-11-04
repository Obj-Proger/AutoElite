namespace AutoElite.Application.TestTemplates;

/// <summary>Decides what of the templates a caller may read.</summary>
internal static class TemplateReaders
{
    /// <summary>
    /// Determines whether the caller reads only active templates: a student who does not also write templates.
    /// </summary>
    /// <param name="currentUser">The caller.</param>
    public static bool ReadsOnlyActive(ICurrentUser currentUser) =>
        !currentUser.IsInAnyRole(UserRole.Administrator, UserRole.TheoryInstructor);
}

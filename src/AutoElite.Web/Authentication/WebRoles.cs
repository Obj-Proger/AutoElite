using AutoElite.Client.Api.Models;

namespace AutoElite.Web.Authentication;

/// <summary>
/// The roles of the users, as the claims of the site name them, for the pages and the menu, which show each user what
/// their roles may do; the API checks the same roles on every request.
/// </summary>
public static class WebRoles
{
    /// <summary>The administrators of the school.</summary>
    public const string Administrator = nameof(UserRole.Administrator);

    /// <summary>The managers, who work with the clients of their branch.</summary>
    public const string Manager = nameof(UserRole.Manager);

    /// <summary>The driving instructors.</summary>
    public const string DrivingInstructor = nameof(UserRole.DrivingInstructor);

    /// <summary>The theory instructors.</summary>
    public const string TheoryInstructor = nameof(UserRole.TheoryInstructor);

    /// <summary>The students.</summary>
    public const string Student = nameof(UserRole.Student);

    /// <summary>The staff of the office, administrators and managers, as the value of <c>Roles</c> of an authorization.</summary>
    public const string Staff = Administrator + "," + Manager;
}

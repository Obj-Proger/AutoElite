using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>The names of the roles of the school, in the language of the application.</summary>
internal static class RoleTexts
{
    /// <summary>Gives the name of a role.</summary>
    /// <param name="role">The role.</param>
    /// <returns>The name.</returns>
    public static string Of(UserRole role) => role switch
    {
        UserRole.Student => AppTexts.Get("Role.Student"),
        UserRole.DrivingInstructor => AppTexts.Get("Role.DrivingInstructor"),
        UserRole.TheoryInstructor => AppTexts.Get("Role.TheoryInstructor"),
        UserRole.Manager => AppTexts.Get("Role.Manager"),
        _ => AppTexts.Get("Role.Administrator")
    };
}

using Microsoft.Extensions.Localization;

namespace AutoElite.Web.Authentication;

/// <summary>The names of the roles in the language of the site.</summary>
internal static class RoleTexts
{
    /// <summary>Gives the name of a role.</summary>
    /// <param name="L">The texts of the site.</param>
    /// <param name="role">The role, as <see cref="WebRoles"/> names it.</param>
    /// <returns>The name of the role, or the role itself when it is unknown.</returns>
    public static string RoleName(this IStringLocalizer<WebTexts> L, string role) => role switch
    {
        WebRoles.Administrator => L["Role.Administrator"],
        WebRoles.Manager => L["Role.Manager"],
        WebRoles.DrivingInstructor => L["Role.DrivingInstructor"],
        WebRoles.TheoryInstructor => L["Role.TheoryInstructor"],
        WebRoles.Student => L["Role.Student"],
        _ => role
    };
}

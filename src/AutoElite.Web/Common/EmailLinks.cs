using Microsoft.AspNetCore.Components;

namespace AutoElite.Web.Common;

/// <summary>
/// The links that the API emails, to accept an invitation, to reset a password and to confirm a new email address: the
/// user and a token in the query of the page.
/// </summary>
internal static class EmailLinks
{
    /// <summary>
    /// Takes the user, the token and the email address out of the address of the page, once the page has read them, in
    /// place of the entry of the history, so that the token is not left in the address bar, in the history of the
    /// browser or on a screen that somebody else sees.
    /// </summary>
    /// <param name="navigation">The navigation of the site.</param>
    public static void Forget(NavigationManager navigation) =>
        navigation.NavigateTo(
            navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
            {
                ["userId"] = null,
                ["token"] = null,
                ["email"] = null
            }),
            replace: true);
}

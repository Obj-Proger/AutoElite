using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// Builds the links to the web client's pages that emails carry. The pages read the parameters from the query string;
/// every value is escaped, since tokens contain characters that have a meaning in addresses.
/// </summary>
internal sealed class ClientLinks(IOptions<ClientOptions> options) : IClientLinks
{
    /// <summary>The page where an invited user sets the first password.</summary>
    public const string InvitationPage = "accept-invitation";

    /// <summary>The page where a user sets a new password.</summary>
    public const string PasswordResetPage = "reset-password";

    /// <summary>The page that confirms a new email address.</summary>
    public const string EmailChangePage = "confirm-email-change";

    /// <inheritdoc />
    public Uri Invitation(Guid userId, string token) =>
        Page(InvitationPage, ("userId", userId.ToString()), ("token", token));

    /// <inheritdoc />
    public Uri PasswordReset(Guid userId, string token) =>
        Page(PasswordResetPage, ("userId", userId.ToString()), ("token", token));

    /// <inheritdoc />
    public Uri EmailChange(Guid userId, Email newEmail, string token) =>
        Page(EmailChangePage, ("userId", userId.ToString()), ("email", newEmail.Value), ("token", token));

    private Uri Page(string page, params (string Name, string Value)[] parameters)
    {
        var baseUrl = options.Value.BaseUrl ?? throw new InvalidOperationException("Client:BaseUrl is not set.");
        var root = baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl : new Uri(baseUrl.AbsoluteUri + "/");
        var query = string.Join('&', parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(parameter.Value)}"));

        return new Uri(root, $"{page}?{query}");
    }
}

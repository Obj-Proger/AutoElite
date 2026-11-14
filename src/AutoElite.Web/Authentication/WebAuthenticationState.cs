using System.Globalization;
using System.Security.Claims;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Web.Authentication;

/// <summary>
/// Tells the components who is signed in: the user of the <see cref="BrowserSession"/>, with their name, email address
/// and roles from their profile, which the API gives; nobody when the session cannot be restored or the API cannot be
/// reached. The state is read again whenever the session changes.
/// </summary>
internal sealed class WebAuthenticationState : AuthenticationStateProvider, IDisposable
{
    /// <summary>The type of the authentication of the signed-in user.</summary>
    public const string AuthenticationType = "AutoElite";

    private static readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly BrowserSession _session;
    private readonly Func<AutoEliteApiClient> _createApi;
    private Task<AuthenticationState>? _state;

    /// <summary>Initializes a new instance of the <see cref="WebAuthenticationState"/> class.</summary>
    /// <param name="session">The session of the website.</param>
    /// <param name="createApi">Creates the client of the API.</param>
    public WebAuthenticationState(BrowserSession session, Func<AutoEliteApiClient> createApi)
    {
        _session = session;
        _createApi = createApi;
        _session.Changed += OnSessionChanged;
    }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state ??= ReadAsync();

    /// <inheritdoc />
    public void Dispose() => _session.Changed -= OnSessionChanged;

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _state = ReadAsync();
        NotifyAuthenticationStateChanged(_state);
    }

    // The name of the user as the site shows it, first name first.
    private static string NameOf(string? firstName, string? lastName) =>
        string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task<AuthenticationState> ReadAsync()
    {
        try
        {
            if (!await _session.RestoreAsync(CancellationToken.None))
                return _anonymous;

            var profile = await _createApi().Api.V1.Me.GetAsync();
            if (profile is not { Id: { } id, Email: { } email })
                return _anonymous;

            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, id.ToString("D", CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, NameOf(profile.FirstName, profile.LastName)),
                new(ClaimTypes.Email, email)
            ];
            claims.AddRange((profile.Roles ?? []).OfType<UserRole>().Select(role => new Claim(ClaimTypes.Role, role.ToString())));

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType)));
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the API nobody is signed in; the state is read again when the session changes.
            _state = null;

            return _anonymous;
        }
    }
}

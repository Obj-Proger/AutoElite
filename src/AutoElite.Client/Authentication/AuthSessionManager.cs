using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Client.Authentication;

/// <summary>
/// The session of the signed-in user: signs in and out, and gives the access token of each request, renewing the
/// session with its refresh token when the access token expires or the API rejects it. One renewal runs at a time and
/// requests that wait for it take its result, since the API ends every session of a user whose refresh token is
/// presented twice. When the API refuses the refresh token, the session is forgotten and <see cref="SessionEnded"/>
/// tells the application to ask the user to sign in again. Without a network the session is kept.
/// </summary>
/// <param name="createAuthenticationApi">Creates the client of the API whose requests carry no access token.</param>
/// <param name="store">Where the session is kept.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class AuthSessionManager(
    Func<AutoEliteApiClient> createAuthenticationApi,
    IAuthSessionStore store,
    TimeProvider timeProvider) : IAccessTokens, IDisposable
{
    // An access token this close to its expiry is renewed before a request, so that it does not expire on the way.
    private static readonly TimeSpan _renewalMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _renewal = new(1, 1);

    /// <summary>Occurs when the API refuses to renew the session, which is then forgotten.</summary>
    public event EventHandler? SessionEnded;

    /// <summary>Tells whether a session is kept whose refresh token has not expired.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="true"/> when the user is signed in.</returns>
    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(cancellationToken) is { } session && session.RefreshTokenExpiresAt > timeProvider.GetUtcNow();

    /// <summary>Signs in with the email address and the password and keeps the new session.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <exception cref="ApiException">The API refused the sign-in; its problem tells why.</exception>
    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var tokens = await createAuthenticationApi().Api.V1.Auth.Login.PostAsync(
            new LoginCommand { Email = email, Password = password }, cancellationToken: cancellationToken);

        await store.SaveAsync(SessionOf(tokens), cancellationToken);
    }

    /// <summary>
    /// Signs out: forgets the session and ends it on the API; without a network it ends there when it expires.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            if (await store.GetAsync(cancellationToken) is not { } session)
                return;

            await store.ClearAsync(cancellationToken);
            try
            {
                await createAuthenticationApi().Api.V1.Auth.Logout.PostAsync(
                    new LogoutCommand { RefreshToken = session.RefreshToken }, cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException)
            {
                // The user is signed out here; the session on the API expires with its refresh token.
            }
        }
        finally
        {
            _renewal.Release();
        }
    }

    /// <summary>Gives the access token for a request, renewed first when it is about to expire.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The access token, or <see langword="null"/> when nobody is signed in.</returns>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (await store.GetAsync(cancellationToken) is not { } session)
            return null;

        return session.AccessTokenExpiresAt - _renewalMargin > timeProvider.GetUtcNow()
            ? session.AccessToken
            : await RenewAsync(session.AccessToken, cancellationToken);
    }

    /// <summary>
    /// Renews the session whose access token expired or was rejected, unless another request has already renewed it.
    /// </summary>
    /// <param name="staleAccessToken">The access token that expired or was rejected.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The new access token, or <see langword="null"/> when the session has ended.</returns>
    public async Task<string?> RenewAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            if (await store.GetAsync(cancellationToken) is not { } session)
                return null;

            if (session.AccessToken != staleAccessToken)
                return session.AccessToken;

            AuthTokens? tokens;
            try
            {
                tokens = await createAuthenticationApi().Api.V1.Auth.Refresh.PostAsync(
                    new RefreshTokensCommand { RefreshToken = session.RefreshToken }, cancellationToken: cancellationToken);
            }
            catch (ApiException exception) when (exception.ResponseStatusCode is 400 or 401)
            {
                await store.ClearAsync(cancellationToken);
                SessionEnded?.Invoke(this, EventArgs.Empty);

                return null;
            }

            var renewed = SessionOf(tokens);
            await store.SaveAsync(renewed, cancellationToken);

            return renewed.AccessToken;
        }
        finally
        {
            _renewal.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _renewal.Dispose();

    private static AuthSession SessionOf(AuthTokens? tokens) =>
        tokens is
        {
            AccessToken: { Value: { } accessToken, ExpiresAtUtc: { } accessTokenExpiresAt },
            RefreshToken: { Value: { } refreshToken, ExpiresAtUtc: { } refreshTokenExpiresAt }
        }
            ? new AuthSession(accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt)
            : throw new InvalidOperationException("The API answered without the tokens of the session.");
}

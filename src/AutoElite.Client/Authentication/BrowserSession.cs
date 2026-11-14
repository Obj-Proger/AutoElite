using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Client.Authentication;

/// <summary>
/// The session of the website. Its refresh token stays in an HttpOnly cookie that the API sets and no script of the
/// page can read; the access token is kept in memory only, so that it is gone with the page. When the page opens, the
/// session is restored from the cookie, if the browser has one; the access token is renewed with the cookie when it
/// expires or the API rejects it. One renewal runs at a time, across the tabs of the browser too
/// (<see cref="ISessionRenewalLock"/>). When the API refuses to renew, the session has ended: <see cref="Changed"/>
/// tells the website to ask the user to sign in again.
/// </summary>
/// <param name="createAuthenticationApi">
/// Creates the client of the API whose requests carry the header of the website (<see cref="WebsiteHeaderHandler"/>)
/// and the cookies of the browser.
/// </param>
/// <param name="timeProvider">The clock.</param>
/// <param name="renewalLock">Keeps the tabs of the browser from renewing at the same moment, if there is one.</param>
public sealed class BrowserSession(
    Func<AutoEliteApiClient> createAuthenticationApi,
    TimeProvider timeProvider,
    ISessionRenewalLock? renewalLock = null) : IAccessTokens, IDisposable
{
    // An access token this close to its expiry is renewed before a request, so that it does not expire on the way.
    private static readonly TimeSpan _renewalMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _renewal = new(1, 1);
    private volatile Access? _access;

    // Whether the session was restored from the cookie, or signed in or out since the page opened.
    private bool _isRestored;

    /// <summary>Occurs when the user signs in or out, or when the session ends because the API refused to renew it.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether the user is signed in on this page.</summary>
    public bool IsSignedIn => _access is not null;

    /// <summary>
    /// Restores the session from the cookie of the browser, once after the page opens; later calls give the state the
    /// session is in.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="true"/> when the user is signed in.</returns>
    /// <exception cref="HttpRequestException">The API could not be reached; the session can be restored later.</exception>
    public async Task<bool> RestoreAsync(CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            if (!_isRestored)
                await RefreshAsync(cancellationToken);

            return _access is not null;
        }
        finally
        {
            _renewal.Release();
        }
    }

    /// <summary>Signs in with the email address and the password; the API keeps the refresh token in its cookie.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <exception cref="ApiException">The API refused the sign-in; its status tells why.</exception>
    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            var access = await createAuthenticationApi().Api.V1.Auth.Browser.Login.PostAsync(
                new LoginCommand { Email = email, Password = password }, cancellationToken: cancellationToken);
            _access = AccessOf(access);
            _isRestored = true;
        }
        finally
        {
            _renewal.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Signs out: the API ends the session and removes its cookie, and the access token is forgotten. When the API
    /// cannot be reached, the user stays signed in, since the cookie would restore the session on the next visit.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <exception cref="HttpRequestException">The API could not be reached; the user is still signed in.</exception>
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            try
            {
                await createAuthenticationApi().Api.V1.Auth.Browser.Logout.PostAsync(cancellationToken: cancellationToken);
            }
            catch (ApiException)
            {
                // The session has already ended on the API.
            }

            _access = null;
            _isRestored = true;
        }
        finally
        {
            _renewal.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!_isRestored)
            await RestoreAsync(cancellationToken);

        if (_access is not { } access)
            return null;

        return access.ExpiresAt - _renewalMargin > timeProvider.GetUtcNow()
            ? access.Token
            : await RenewAsync(access.Token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> RenewAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await _renewal.WaitAsync(cancellationToken);
        try
        {
            if (_access is not { } access)
                return null;

            if (access.Token != staleAccessToken)
                return access.Token;

            await RefreshAsync(cancellationToken);

            return _access?.Token;
        }
        finally
        {
            _renewal.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _renewal.Dispose();

    // Renews the access token with the cookie; the caller holds the renewal. Without a network nothing changes.
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var wasSignedIn = _access is not null;
        BrowserAccess? renewed = null;
        var hasEnded = false;
        async Task Refresh()
        {
            try
            {
                renewed = await createAuthenticationApi().Api.V1.Auth.Browser.Refresh.PostAsync(cancellationToken: cancellationToken);
            }
            catch (ApiException exception) when (exception.ResponseStatusCode is 400 or 401)
            {
                hasEnded = true;
            }
        }

        if (renewalLock is null)
            await Refresh();
        else
            await renewalLock.RunAsync(Refresh, cancellationToken);

        _isRestored = true;
        if (!hasEnded)
        {
            _access = AccessOf(renewed);

            return;
        }

        _access = null;
        if (wasSignedIn)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    private static Access AccessOf(BrowserAccess? access) =>
        access is { AccessToken: { Value: { } token, ExpiresAtUtc: { } expiresAt } }
            ? new Access(token, expiresAt)
            : throw new InvalidOperationException("The API answered without the access token of the session.");

    // The access token of the session and when it expires.
    private sealed record Access(string Token, DateTimeOffset ExpiresAt);
}

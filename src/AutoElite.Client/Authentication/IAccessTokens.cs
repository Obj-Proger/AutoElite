namespace AutoElite.Client.Authentication;

/// <summary>
/// Gives the access token of the signed-in user for each request to the API and renews it when it expires or the API
/// rejects it: <see cref="AuthSessionManager"/> in the mobile application, which keeps the whole session, and the
/// session of the website, whose refresh token stays in a cookie the page cannot read.
/// </summary>
public interface IAccessTokens
{
    /// <summary>Gives the access token for a request, renewed first when it is about to expire.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The access token, or <see langword="null"/> when nobody is signed in.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Renews the session whose access token expired or was rejected, unless another request has already renewed it.
    /// </summary>
    /// <param name="staleAccessToken">The access token that expired or was rejected.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The new access token, or <see langword="null"/> when the session has ended.</returns>
    Task<string?> RenewAsync(string staleAccessToken, CancellationToken cancellationToken);
}

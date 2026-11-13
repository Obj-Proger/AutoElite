using Microsoft.Kiota.Abstractions;

namespace AutoElite.Client.Offline;

/// <summary>Tells the failures of a request that end when the network returns from those the API decided.</summary>
internal static class ApiFailures
{
    /// <summary>
    /// Tells whether a request failed for now only: the network or the API could not be reached, the request timed
    /// out, the API was busy or failing, or the session has to be renewed by signing in again.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="cancellationToken">The token of the request; a request it cancelled did not fail.</param>
    /// <returns><see langword="true"/> when the same request may succeed later.</returns>
    public static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            ApiException api => api.ResponseStatusCode is 401 or 408 or 429 or >= 500,
            HttpRequestException => true,
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            _ => false
        };

    /// <summary>
    /// Tells whether the API refused a request for good: what it asked for does not exist, is not allowed or conflicts
    /// with the data on the API, which may have changed since the user saw it.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns><see langword="true"/> when sending the same request again would be refused again.</returns>
    public static bool IsRefusal(ApiException exception) =>
        exception.ResponseStatusCode is >= 400 and < 500 and not (401 or 408 or 429);
}

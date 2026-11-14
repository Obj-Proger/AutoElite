namespace AutoElite.Client.Authentication;

/// <summary>
/// Lets one instance of the application at a time renew a session that several instances share, such as the tabs of a
/// browser, which share the cookie of the website: the API accepts a refresh token once and ends every session of a
/// user whose refresh token is presented twice, so two tabs must not renew at the same moment.
/// </summary>
public interface ISessionRenewalLock
{
    /// <summary>Runs a renewal once no other instance of the application is renewing.</summary>
    /// <param name="renewal">The renewal.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the wait.</param>
    /// <returns>A task that completes when the renewal has run.</returns>
    Task RunAsync(Func<Task> renewal, CancellationToken cancellationToken);
}

using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Authentication;
using AutoElite.Client.Offline;
using AutoElite.Client.Realtime;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Session;

/// <summary>
/// The user of the application on the device: signing in, which makes the offline data theirs, their profile, read
/// from the API or from the device, signing out, which forgets the offline data, and changing the school.
/// </summary>
/// <param name="sessions">The session of the signed-in user.</param>
/// <param name="offline">The offline database.</param>
/// <param name="cache">What the user last read.</param>
/// <param name="queue">The actions that wait to be sent.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="realtime">The connection to the hub of the API, closed when the user signs out.</param>
public sealed class UserSession(
    AuthSessionManager sessions,
    OfflineDatabase offline,
    OfflineCache cache,
    OfflineQueue queue,
    Func<AutoEliteApiClient> createApi,
    RealtimeConnection realtime)
{
    private const string ProfileKey = "me";

    /// <summary>Tells whether the user is signed in.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="true"/> when the user is signed in.</returns>
    public Task<bool> IsSignedInAsync(CancellationToken cancellationToken) => sessions.IsSignedInAsync(cancellationToken);

    /// <summary>
    /// Signs in a student or an instructor and makes the offline data on the device theirs; the staff of the school,
    /// who work on its website, are signed out again.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>How signing in ended.</returns>
    public async Task<SignInOutcome> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        UserProfile? profile;
        try
        {
            await sessions.SignInAsync(email, password, cancellationToken);
            profile = await createApi().Api.V1.Me.GetAsync(cancellationToken: cancellationToken);
        }
        catch (ApiException exception)
        {
            await sessions.SignOutAsync(cancellationToken);

            return exception.ResponseStatusCode switch
            {
                400 or 401 => SignInOutcome.WrongCredentials,
                403 => SignInOutcome.Blocked,
                429 => SignInOutcome.TooManyAttempts,
                >= 500 => SignInOutcome.Unreachable,
                _ => SignInOutcome.Failed
            };
        }
        catch (Exception exception) when (exception is HttpRequestException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            await sessions.SignOutAsync(cancellationToken);

            return SignInOutcome.Unreachable;
        }

        if (profile is not { Id: { } userId, Roles: { } roles }
            || !roles.Exists(role => role is UserRole.Student or UserRole.DrivingInstructor or UserRole.TheoryInstructor))
        {
            await sessions.SignOutAsync(cancellationToken);

            return SignInOutcome.NotForThisApp;
        }

        await offline.ClaimAsync(userId, cancellationToken);

        return SignInOutcome.SignedIn;
    }

    /// <summary>Reads the profile of the signed-in user from the API or, without a network, from the device.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The profile.</returns>
    public Task<Cached<UserProfile>> GetProfileAsync(CancellationToken cancellationToken) =>
        cache.GetAsync(
            ProfileKey,
            token => createApi().Api.V1.Me.GetAsync(cancellationToken: token),
            UserProfile.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Counts the actions that wait to be sent, which signing out would lose.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The number of actions.</returns>
    public async Task<int> CountWaitingActionsAsync(CancellationToken cancellationToken) =>
        (await queue.ListAsync(cancellationToken)).Count(action => !action.IsRejected);

    /// <summary>Signs out, closes the connection to the hub and forgets the offline data.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the user is signed out.</returns>
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await realtime.StopAsync(cancellationToken);
        await sessions.SignOutAsync(cancellationToken);
        await offline.ForgetAsync(cancellationToken);
    }

    /// <summary>Signs out, forgets the offline data and the address of the school, so that the user gives another one.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the school is forgotten.</returns>
    public async Task ChangeSchoolAsync(CancellationToken cancellationToken)
    {
        await SignOutAsync(cancellationToken);
        ApiAddress.Clear();
    }
}

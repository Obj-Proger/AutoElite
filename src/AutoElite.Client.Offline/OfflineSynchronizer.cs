using AutoElite.Client.Api;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Client.Offline;

/// <summary>
/// Sends the actions of the <see cref="OfflineQueue"/> to the API, one at a time in the order they were taken. An
/// action the API accepts leaves the queue; one it refuses is marked as rejected and the next one is sent, since the
/// data on the API may have changed while the device was offline. When the API cannot be reached, or the user has to
/// sign in again, the synchronization stops and the action waits for the next one. One synchronization runs at a time.
/// </summary>
/// <param name="queue">The queue of actions.</param>
/// <param name="createApi">Creates the client of the API, whose requests carry the access token of the session.</param>
public sealed class OfflineSynchronizer(OfflineQueue queue, Func<AutoEliteApiClient> createApi) : IDisposable
{
    private readonly SemaphoreSlim _running = new(1, 1);

    /// <summary>
    /// Takes an action: adds it to the queue and sends the queue, so that it reaches the API at once when it can be
    /// reached and later otherwise.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>What the synchronization did.</returns>
    public async Task<SynchronizationResult> SubmitAsync(OfflineAction action, CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync(action, cancellationToken);

        return await SynchronizeAsync(cancellationToken);
    }

    /// <summary>Sends the waiting actions, the earliest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>What the synchronization did.</returns>
    public async Task<SynchronizationResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        await _running.WaitAsync(cancellationToken);
        try
        {
            var api = createApi();
            var (sent, rejected) = (0, 0);
            while (await queue.NextWaitingAsync(cancellationToken) is { } pending)
            {
                // Once the API has answered, the queue records the answer even when the caller has stopped waiting, so
                // that an accepted action is not sent twice.
                try
                {
                    await pending.Action.SendAsync(api, cancellationToken);
                }
                catch (ApiException exception) when (ApiFailures.IsRefusal(exception))
                {
                    await queue.RejectAsync(pending.Id, exception.ResponseStatusCode, CancellationToken.None);
                    rejected++;
                    continue;
                }
                catch (Exception exception) when (ApiFailures.IsTransient(exception, cancellationToken))
                {
                    await queue.RecordAttemptAsync(pending.Id, CancellationToken.None);

                    return new SynchronizationResult(sent, rejected, IsComplete: false);
                }

                await queue.CompleteAsync(pending.Id, CancellationToken.None);
                sent++;
            }

            return new SynchronizationResult(sent, rejected, IsComplete: true);
        }
        finally
        {
            _running.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _running.Dispose();
}

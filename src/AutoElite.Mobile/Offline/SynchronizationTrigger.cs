using AutoElite.Client.Offline;
using Microsoft.Extensions.Logging;

namespace AutoElite.Mobile.Offline;

/// <summary>
/// Sends the actions the user took without a network when the application starts or comes back to the foreground and
/// when the device connects to a network.
/// </summary>
/// <param name="synchronizer">The synchronizer of the offline queue.</param>
/// <param name="connectivity">The network state of the device.</param>
/// <param name="logger">The logger.</param>
public sealed partial class SynchronizationTrigger(
    OfflineSynchronizer synchronizer, IConnectivity connectivity, ILogger<SynchronizationTrigger> logger)
{
    private bool _isStarted;

    /// <summary>Starts watching the network and sends the queue.</summary>
    public void Start()
    {
        if (!_isStarted)
        {
            connectivity.ConnectivityChanged += (_, _) => Synchronize();
            _isStarted = true;
        }

        Synchronize();
    }

    /// <summary>Sends the queue in the background, unless the device has no network at all.</summary>
    public void Synchronize()
    {
        // A school's API may be on the local network, so any network is worth a try; a failed try costs one request.
        if (connectivity.NetworkAccess != NetworkAccess.None)
            _ = SynchronizeAsync();
    }

    private async Task SynchronizeAsync()
    {
        try
        {
            await synchronizer.SynchronizeAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // The actions stay in the queue for the next synchronization.
            LogSynchronizationFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending the offline actions failed")]
    private static partial void LogSynchronizationFailed(ILogger logger, Exception exception);
}

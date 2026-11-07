using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// Runs the outbox processor for as long as the host runs: at once again while messages keep coming in full batches,
/// otherwise after <see cref="OutboxOptions.PollingInterval"/>. A failure of a run, such as a lost database, is
/// logged and the worker carries on after the pause, since an exception would stop the host.
/// </summary>
internal sealed partial class OutboxWorker(
    OutboxProcessor processor,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The worker outlives any failure of a run; the next run tries again.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var taken = 0;
            try
            {
                taken = await processor.ProcessAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogRunFailed(logger, exception);
            }

            if (taken < options.Value.BatchSize)
                await Task.Delay(options.Value.PollingInterval, timeProvider, stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A run of the outbox processor failed")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}

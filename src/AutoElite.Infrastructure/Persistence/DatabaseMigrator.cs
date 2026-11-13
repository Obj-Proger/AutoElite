using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date with the migrations when the host starts, before the outbox, the mail and the
/// scheduled jobs use it and before the host takes requests. Hosts that start at once wait for each other, since
/// EF Core locks the database while it migrates. A failure stops the host: it cannot work on an older schema.
/// </summary>
internal sealed partial class DatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseMigrator> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MigrateOnStartup)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Database;
        var pending = (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
            return;

        LogMigrating(logger, pending.Count, pending[^1]);
        await database.MigrateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying {Count} pending migrations, up to {LastMigration}")]
    private static partial void LogMigrating(ILogger logger, int count, string lastMigration);
}

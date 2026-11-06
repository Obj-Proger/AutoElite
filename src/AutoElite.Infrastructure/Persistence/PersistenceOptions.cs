using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>The database options every <see cref="AutoEliteDbContext"/> is built with, at run time, in tests and in the tools.</summary>
internal static class PersistenceOptions
{
    /// <summary>The table EF Core records the applied migrations in.</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>
    /// Connects to PostgreSQL and applies the naming of the schema; the model is cached per chat cipher
    /// (<see cref="AutoEliteModelCacheKeyFactory"/>). On every save, domain events move into the outbox,
    /// and a change to a child entity also advances the row version of its aggregate root; the further interceptors,
    /// such as the audit, run after these.
    /// </summary>
    /// <param name="builder">The options to configure.</param>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <param name="interceptors">Further interceptors, such as the audit, if any.</param>
    /// <returns>The same builder, so that a typed builder stays typed.</returns>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    public static TBuilder UseAutoElite<TBuilder>(
        this TBuilder builder, string connectionString, params IInterceptor[] interceptors)
        where TBuilder : DbContextOptionsBuilder
    {
        builder
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, AutoEliteModelCacheKeyFactory>()
            .AddInterceptors(
                interceptors.Prepend(AggregateVersionInterceptor.Instance).Prepend(DomainEventOutboxInterceptor.Instance));

        return builder;
    }
}

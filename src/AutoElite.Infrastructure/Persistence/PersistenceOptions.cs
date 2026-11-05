using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>The database options every <see cref="AutoEliteDbContext"/> is built with, at run time, in tests and in the tools.</summary>
internal static class PersistenceOptions
{
    /// <summary>The table EF Core records the applied migrations in.</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Connects to PostgreSQL and applies the naming of the schema.</summary>
    /// <param name="builder">The options to configure.</param>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <param name="interceptors">The interceptors that run on every save, if any.</param>
    /// <returns>The same builder, so that a typed builder stays typed.</returns>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    public static TBuilder UseAutoElite<TBuilder>(
        this TBuilder builder, string connectionString, params IInterceptor[] interceptors)
        where TBuilder : DbContextOptionsBuilder
    {
        builder
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptors);

        return builder;
    }
}

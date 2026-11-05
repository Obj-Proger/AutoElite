using AutoElite.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure.Tests.Database;

/// <summary>
/// A database of its own for one test class, with the schema of the model; dropped when the class has run. Tests of a
/// class share it, so each test uses records of its own and does not count on the others being absent.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private string _connectionString = null!;

    /// <summary>Creates a context on the database, as the application would build it for a request.</summary>
    /// <param name="currentUser">The user of the request; the system by default.</param>
    /// <param name="timeProvider">The clock; the system clock by default.</param>
    public AutoEliteDbContext CreateContext(ICurrentUser? currentUser = null, TimeProvider? timeProvider = null) =>
        new(new DbContextOptionsBuilder<AutoEliteDbContext>()
            .UseAutoElite(
                _connectionString,
                new AuditInterceptor(currentUser ?? TestCurrentUser.System, timeProvider ?? TimeProvider.System))
            .Options);

    /// <summary>
    /// Builds the services of the infrastructure on the database, as a host would, for tests of components that are
    /// resolved rather than constructed, such as the identity service.
    /// </summary>
    /// <param name="currentUser">The user of every request; the system by default.</param>
    public ServiceProvider CreateServices(ICurrentUser? currentUser = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = _connectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(currentUser ?? TestCurrentUser.System);
        services.AddSingleton(TimeProvider.System);
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _connectionString = await TestPostgreSql.CreateDatabaseAsync(TestContext.Current.CancellationToken);

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await TestPostgreSql.DropDatabaseAsync(_connectionString);
}

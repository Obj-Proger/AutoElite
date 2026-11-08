using AutoElite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoElite.API.Tests;

/// <summary>
/// The API as it runs, on a database of its own created by the migrations when the host starts, with the settings a
/// deployment gives it: test keys, and folders of its own for the files and the outgoing mail. One host serves the
/// whole run (<see cref="ApiCollection"/>), so each test uses records of its own.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 32 bytes of 0x02 in Base64, the chat key of the tests.
    private const string ChatKey = "AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"autoelite-api-tests-{Guid.NewGuid():N}");

    private string _connectionString = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync() =>
        _connectionString = await TestPostgreSql.CreateDatabaseAsync(TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await TestPostgreSql.DropDatabaseAsync(_connectionString);
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{Infrastructure.DependencyInjection.ConnectionStringName}", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "a signing key of the API tests, longer than 32 bytes");
        builder.UseSetting("Client:BaseUrl", "http://localhost:5173");
        builder.UseSetting("ChatEncryption:CurrentKeyId", "test_1");
        builder.UseSetting("ChatEncryption:Keys:test_1", ChatKey);
        builder.UseSetting("Email:Mode", "PickupDirectory");
        builder.UseSetting("Email:FromAddress", "no-reply@autoelite.test");
        builder.UseSetting("Email:PickupDirectory", Path.Combine(_folder, "mail"));
        builder.UseSetting("FileStorage:RootPath", Path.Combine(_folder, "files"));

        // One Hangfire worker is enough for the tests and keeps the connections to the database few.
        builder.UseSetting("BackgroundJobs:WorkerCount", "1");

        // First of the hosted services: the outbox, the mail and Hangfire query the schema as soon as they start.
        builder.ConfigureServices(services => services.Insert(0, ServiceDescriptor.Singleton<IHostedService, DatabaseMigrator>()));
    }

    /// <summary>Brings the database up to date when the host starts.</summary>
    /// <param name="scopeFactory">The factory of the scope with the database context.</param>
    private sealed class DatabaseMigrator(IServiceScopeFactory scopeFactory) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

/// <summary>The tests that share the host of <see cref="ApiFactory"/>.</summary>
[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;

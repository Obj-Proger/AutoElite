using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutoElite.Application.Common.Abstractions;
using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;
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

    /// <summary>
    /// Registers a user with roles and a sign-in account, as an invitation does, and makes the token of the invitation
    /// the user would get by email.
    /// </summary>
    /// <param name="roles">The roles of the user.</param>
    /// <returns>The user, the address and the token of the invitation.</returns>
    public async Task<InvitedUser> InviteAsync(params UserRole[] roles)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var user = User.Create(
                FullName.Create("Тестов", "Тест").Value,
                Email.Create($"{Guid.NewGuid():N}@example.com").Value,
                null,
                null,
                DateTime.UtcNow)
            .Value;

        await using (var scope = Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            services.GetRequiredService<IUserRepository>().Add(user);
            foreach (var role in roles)
                services.GetRequiredService<IUserRoleRepository>().Add(UserRoleAssignment.Create(user.Id, role).Value);

            await services.GetRequiredService<IIdentityService>().CreateAccountAsync(user.Id, user.Email, cancellationToken);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        await using (var scope = Services.CreateAsyncScope())
        {
            var token = await scope.ServiceProvider.GetRequiredService<IIdentityService>()
                .GenerateInvitationTokenAsync(user.Id, cancellationToken);

            return new InvitedUser(user.Id, user.Email.Value, token);
        }
    }

    /// <summary>
    /// Registers a user with roles, accepts the invitation and signs in through the API, and gives a client that sends
    /// the access token of the session with every request.
    /// </summary>
    /// <param name="roles">The roles of the user.</param>
    /// <returns>The client of the user and the user.</returns>
    public async Task<(HttpClient Client, InvitedUser User)> SignInAsync(params UserRole[] roles)
    {
        const string password = "Correct-horse-7";
        var cancellationToken = TestContext.Current.CancellationToken;
        var user = await InviteAsync(roles);
        var client = CreateClient();

        using var accepted = await client.PostAsJsonAsync(
            "/api/v1/auth/invitation", new { user.UserId, Token = user.InvitationToken, Password = password }, cancellationToken);
        accepted.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { user.Email, Password = password }, cancellationToken);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>(ApiJson.Options, cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken.Value);

        return (client, user);
    }

    /// <summary>Signs in a new user with roles, as <see cref="SignInAsync"/> does, and gives only the client.</summary>
    /// <param name="roles">The roles of the user.</param>
    /// <returns>The client of the user.</returns>
    public async Task<HttpClient> SignedInClientAsync(params UserRole[] roles) => (await SignInAsync(roles)).Client;

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

/// <summary>A user registered by <see cref="ApiFactory.InviteAsync"/>.</summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="Email">The email address, the sign-in name.</param>
/// <param name="InvitationToken">The token of the invitation link.</param>
public sealed record InvitedUser(Guid UserId, string Email, string InvitationToken);

/// <summary>The tests that share the host of <see cref="ApiFactory"/>.</summary>
[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;

using AutoElite.Client;
using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using AutoElite.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.API.Tests.Client;

/// <summary>The session of the signed-in user in the client of the API.</summary>
[Collection(nameof(ApiCollection))]
public sealed class AuthenticationTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A user with a password, and the services of a client of the API on the test server.
    private async Task<(ServiceProvider Services, InvitedUser User)> PrepareAsync()
    {
        var (client, user) = await api.SignInAsync(UserRole.Student);
        client.Dispose();
        var services = new ServiceCollection()
            .AddAutoEliteClient(api.Server.BaseAddress, http => http.ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler()))
            .BuildServiceProvider();

        return (services, user);
    }

    private static async Task<AuthSession> SignInAsync(ServiceProvider services, InvitedUser user)
    {
        await services.GetRequiredService<AuthSessionManager>().SignInAsync(user.Email, ApiFactory.Password, CancellationToken);

        return (await services.GetRequiredService<IAuthSessionStore>().GetAsync(CancellationToken))!;
    }

    private static async Task<string?> ReadOwnEmailAsync(ServiceProvider services) =>
        (await services.GetRequiredService<AutoEliteApiClient>().Api.V1.Me.GetAsync(cancellationToken: CancellationToken))?.Email;

    [Fact]
    public async Task SignedInUser_ReadsTheirProfile()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;

        await SignInAsync(services, user);

        (await ReadOwnEmailAsync(services)).Should().Be(user.Email);
        (await services.GetRequiredService<AuthSessionManager>().IsSignedInAsync(CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task RejectedAccessToken_IsRenewedAndTheRequestSentAgain()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;
        var store = services.GetRequiredService<IAuthSessionStore>();
        var session = await SignInAsync(services, user);
        await store.SaveAsync(session with { AccessToken = "not-a-token" }, CancellationToken);

        var email = await ReadOwnEmailAsync(services);

        email.Should().Be(user.Email);
        var renewed = await store.GetAsync(CancellationToken);
        renewed!.RefreshToken.Should().NotBe(session.RefreshToken);
    }

    [Fact]
    public async Task ExpiringAccessToken_IsRenewedBeforeTheRequest()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;
        var store = services.GetRequiredService<IAuthSessionStore>();
        var session = await SignInAsync(services, user);
        await store.SaveAsync(session with { AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(10) }, CancellationToken);

        var email = await ReadOwnEmailAsync(services);

        email.Should().Be(user.Email);
        (await store.GetAsync(CancellationToken))!.AccessToken.Should().NotBe(session.AccessToken);
    }

    [Fact]
    public async Task RequestsAtOnce_RenewTheSessionOnce()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;
        var store = services.GetRequiredService<IAuthSessionStore>();
        var session = await SignInAsync(services, user);
        await store.SaveAsync(session with { AccessToken = "not-a-token" }, CancellationToken);

        // A second renewal would present the used refresh token again, and the API would end every session of the user.
        var emails = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ReadOwnEmailAsync(services)));

        emails.Should().AllBe(user.Email);
    }

    [Fact]
    public async Task RefusedRenewal_EndsTheSession()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;
        var store = services.GetRequiredService<IAuthSessionStore>();
        var sessions = services.GetRequiredService<AuthSessionManager>();
        var session = await SignInAsync(services, user);
        await store.SaveAsync(session with { AccessToken = "not-a-token", RefreshToken = "not-a-refresh-token" }, CancellationToken);
        var ended = false;
        sessions.SessionEnded += (_, _) => ended = true;

        var reading = () => ReadOwnEmailAsync(services);

        (await reading.Should().ThrowAsync<ApiException>()).Which.ResponseStatusCode.Should().Be(401);
        ended.Should().BeTrue();
        (await store.GetAsync(CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task SignOut_ForgetsTheSessionAndEndsItOnTheApi()
    {
        var (services, user) = await PrepareAsync();
        await using var disposal = services;
        var store = services.GetRequiredService<IAuthSessionStore>();
        var sessions = services.GetRequiredService<AuthSessionManager>();
        var session = await SignInAsync(services, user);

        await sessions.SignOutAsync(CancellationToken);

        (await store.GetAsync(CancellationToken)).Should().BeNull();
        await store.SaveAsync(session with { AccessToken = "not-a-token" }, CancellationToken);
        var reading = () => ReadOwnEmailAsync(services);
        (await reading.Should().ThrowAsync<ApiException>()).Which.ResponseStatusCode.Should().Be(401);
    }
}

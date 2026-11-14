using AutoElite.Client.Authentication;

namespace AutoElite.API.Tests.Client;

public sealed class SecretAuthSessionStoreTests
{
    private static readonly AuthSession _session = new(
        "access",
        new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero),
        "refresh",
        new DateTimeOffset(2026, 12, 13, 10, 0, 0, TimeSpan.Zero));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task KeptSession_IsReadByTheNextStartOfTheApplication()
    {
        var secrets = new MemorySecretStore();
        using (var store = new SecretAuthSessionStore(secrets))
            await store.SaveAsync(_session, CancellationToken);

        using var restarted = new SecretAuthSessionStore(secrets);

        (await restarted.GetAsync(CancellationToken)).Should().Be(_session);
    }

    [Fact]
    public async Task Session_IsReadFromTheSecretsOnce()
    {
        var secrets = new MemorySecretStore();
        await secrets.SetAsync(SecretAuthSessionStore.Key, """
            {
              "accessToken": "a",
              "accessTokenExpiresAt": "2026-11-13T10:00:00+00:00",
              "refreshToken": "r",
              "refreshTokenExpiresAt": "2026-12-13T10:00:00+00:00"
            }
            """);
        using var store = new SecretAuthSessionStore(secrets);

        await store.GetAsync(CancellationToken);
        var session = await store.GetAsync(CancellationToken);

        session!.RefreshToken.Should().Be("r");
        secrets.Reads.Should().Be(1);
    }

    [Fact]
    public async Task UnreadableSession_IsForgotten()
    {
        var secrets = new MemorySecretStore();
        await secrets.SetAsync(SecretAuthSessionStore.Key, "not json");
        using var store = new SecretAuthSessionStore(secrets);

        var session = await store.GetAsync(CancellationToken);

        session.Should().BeNull();
        (await secrets.GetAsync(SecretAuthSessionStore.Key)).Should().BeNull();
    }

    [Fact]
    public async Task ClearedSession_IsGoneFromTheSecrets()
    {
        var secrets = new MemorySecretStore();
        using var store = new SecretAuthSessionStore(secrets);
        await store.SaveAsync(_session, CancellationToken);

        await store.ClearAsync(CancellationToken);

        (await store.GetAsync(CancellationToken)).Should().BeNull();
        (await secrets.GetAsync(SecretAuthSessionStore.Key)).Should().BeNull();
    }
}

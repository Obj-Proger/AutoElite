using AutoElite.Client.Api;
using AutoElite.Client.Offline;
using AutoElite.Domain.Enums;
using Profile = AutoElite.Client.Api.Models.UserProfile;

namespace AutoElite.API.Tests.Client.Offline;

/// <summary>What the user read from the API, shown on the device without a network.</summary>
[Collection(nameof(ApiCollection))]
public sealed class OfflineCacheTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Task<Cached<Profile>> ReadProfileAsync(TestDevice device) =>
        device.Get<OfflineCache>().GetAsync(
            "me",
            token => device.Get<AutoEliteApiClient>().Api.V1.Me.GetAsync(cancellationToken: token),
            Profile.CreateFromDiscriminatorValue,
            CancellationToken);

    [Fact]
    public async Task ProfileReadOnline_IsShownFromTheDeviceWithoutANetwork()
    {
        var (client, user) = await api.SignInAsync(UserRole.Student);
        client.Dispose();
        await using var device = new TestDevice(api);
        await device.SignInAsync(user);

        var online = await ReadProfileAsync(device);
        device.IsOnline = false;
        var offline = await ReadProfileAsync(device);

        online.IsFromCache.Should().BeFalse();
        offline.IsFromCache.Should().BeTrue();
        offline.SavedAt.Should().Be(online.SavedAt);
        offline.Value.Id.Should().Be(user.UserId);
        offline.Value.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task ReadWithoutANetwork_FailsWhenNothingIsKept()
    {
        await using var device = new TestDevice(api) { IsOnline = false };

        var reading = () => ReadProfileAsync(device);

        await reading.Should().ThrowAsync<HttpRequestException>();
    }
}

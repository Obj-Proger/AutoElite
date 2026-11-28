using System.Net;
using AutoElite.Application.Notifications;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The devices of the users that receive push notifications.</summary>
[Collection(nameof(ApiCollection))]
public sealed class PushDevicesControllerTests(ApiFactory api)
{
    private async Task<PushDevice?> DeviceAsync(string token)
    {
        await using var scope = api.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IPushDeviceRepository>()
            .FindByTokenAsync(token, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Register_ThenAnotherUserOnTheDevice_PassesTheDeviceToThem()
    {
        var token = $"fcm-{Guid.NewGuid():N}";
        var (first, firstUser) = await api.SignInAsync(UserRole.Student);
        using var firstClient = first;
        var (second, secondUser) = await api.SignInAsync(UserRole.Manager);
        using var secondClient = second;

        using var registered = await first.SendJsonAsync(
            HttpMethod.Post, "/api/v1/push-devices", new { Token = token, Platform = PushPlatform.Android });
        var afterFirst = await DeviceAsync(token);
        using var taken = await second.SendJsonAsync(
            HttpMethod.Post, "/api/v1/push-devices", new { Token = token, Platform = PushPlatform.Android });
        var afterSecond = await DeviceAsync(token);

        registered.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterFirst!.UserId.Should().Be(firstUser.UserId);
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterSecond!.UserId.Should().Be(secondUser.UserId);
    }

    [Fact]
    public async Task Unregister_RemovesOnlyTheDeviceOfTheUser()
    {
        var token = $"fcm-{Guid.NewGuid():N}";
        using var owner = await api.SignedInClientAsync(UserRole.Student);
        using var stranger = await api.SignedInClientAsync(UserRole.Student);
        using var registered = await owner.SendJsonAsync(
            HttpMethod.Post, "/api/v1/push-devices", new { Token = token, Platform = PushPlatform.Android });

        using var refused = await stranger.SendJsonAsync(HttpMethod.Post, "/api/v1/push-devices/unregister", new { Token = token });
        var afterStranger = await DeviceAsync(token);
        using var removed = await owner.SendJsonAsync(HttpMethod.Post, "/api/v1/push-devices/unregister", new { Token = token });
        var afterOwner = await DeviceAsync(token);

        refused.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterStranger.Should().NotBeNull();
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterOwner.Should().BeNull();
    }

    [Fact]
    public async Task Register_WithoutAToken_IsABadRequest()
    {
        using var client = await api.SignedInClientAsync(UserRole.Student);

        using var response = await client.SendJsonAsync(
            HttpMethod.Post, "/api/v1/push-devices", new { Token = " ", Platform = PushPlatform.Android });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

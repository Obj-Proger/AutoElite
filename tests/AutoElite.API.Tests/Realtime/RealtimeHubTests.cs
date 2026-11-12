using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using AutoElite.Domain.Enums;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AutoElite.API.Tests.Realtime;

/// <summary>The updates signed-in users receive at once through the SignalR hub.</summary>
[Collection(nameof(ApiCollection))]
public sealed class RealtimeHubTests(ApiFactory api)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A connection over a WebSocket of the test server, which carries the access token in the address as browsers do.
    private HubConnection Connect(string? accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(api.Server.BaseAddress, "hubs/realtime"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.WebSocketFactory = (context, cancellationToken) =>
                    new ValueTask<WebSocket>(
                        api.Server.CreateWebSocketClient().ConnectAsync(WithAccessToken(context.Uri, accessToken), cancellationToken));
                options.AccessTokenProvider = () => Task.FromResult(accessToken);
            })
            .Build();

    // The address of the WebSocket with the access token in the access_token parameter, where browsers send it.
    private static Uri WithAccessToken(Uri uri, string? accessToken) =>
        accessToken is null || uri.Query.Contains("access_token=", StringComparison.Ordinal)
            ? uri
            : new Uri($"{uri}{(uri.Query.Length == 0 ? "?" : "&")}access_token={Uri.EscapeDataString(accessToken)}");

    private static TaskCompletionSource<JsonElement> Listen(HubConnection connection, string method)
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(method, update => received.TrySetResult(update));

        return received;
    }

    [Fact]
    public async Task Recipient_ReceivesTheMessageAndTheNotificationAtOnce()
    {
        var (sender, senderUser) = await api.SignInAsync(UserRole.Student);
        using var senderClient = sender;
        var (recipient, recipientUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var recipientClient = recipient;
        await using var connection = Connect(recipient.DefaultRequestHeaders.Authorization!.Parameter);
        var message = Listen(connection, "ChatMessageReceived");
        var notification = Listen(connection, "NotificationReceived");
        await connection.StartAsync(CancellationToken);

        var messageId = await sender.CreateAsync(
            "/api/v1/chats/direct/messages", new { RecipientId = recipientUser.UserId, Text = "Я подъеду к 10:00" });
        var messageUpdate = await message.Task.WaitAsync(_timeout, CancellationToken);
        var notificationUpdate = await notification.Task.WaitAsync(_timeout, CancellationToken);

        messageUpdate.GetProperty("messageId").GetGuid().Should().Be(messageId);
        messageUpdate.GetProperty("senderId").GetGuid().Should().Be(senderUser.UserId);
        notificationUpdate.GetProperty("type").GetString().Should().Be(nameof(NotificationType.NewMessage));
        var unread = await recipient.GetJsonAsync("/api/v1/notifications?isRead=false");
        unread.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().Contain(notificationUpdate.GetProperty("notificationId").GetGuid());
    }

    [Fact]
    public async Task Connection_WithoutAnAccessToken_IsRefused()
    {
        await using var connection = Connect(null);

        var connecting = () => connection.StartAsync(CancellationToken);

        (await connecting.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using AutoElite.API.Tests.Client.Offline;
using AutoElite.Client.Authentication;
using AutoElite.Client.Realtime;
using AutoElite.Domain.Enums;
using Microsoft.AspNetCore.Http.Connections;

namespace AutoElite.API.Tests.Client;

/// <summary>The connection of a signed-in user of the client to the SignalR hub.</summary>
[Collection(nameof(ApiCollection))]
public sealed class RealtimeConnectionTests(ApiFactory api)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignedInUser_ReceivesTheMessageAndTheNotificationAtOnce()
    {
        var (sender, senderUser) = await api.SignInAsync(UserRole.Student);
        using var senderClient = sender;
        var (recipient, recipientUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        recipient.Dispose();
        await using var device = new TestDevice(api);
        await device.SignInAsync(recipientUser);
        await using var connection = new RealtimeConnection(
            () => api.Server.BaseAddress,
            device.Get<AuthSessionManager>(),
            options =>
            {
                // The test server serves HTTP only, so the connection polls rather than opening a WebSocket.
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
            });
        var message = new TaskCompletionSource<ChatMessageSignal>(TaskCreationOptions.RunContinuationsAsynchronously);
        var notification = new TaskCompletionSource<NotificationSignal>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ChatMessageReceived += (_, signal) => message.TrySetResult(signal);
        connection.NotificationReceived += (_, signal) => notification.TrySetResult(signal);

        await connection.StartAsync(CancellationToken);
        await connection.StartAsync(CancellationToken);
        var messageId = await sender.CreateAsync(
            "/api/v1/chats/direct/messages", new { RecipientId = recipientUser.UserId, Text = "Буду через 5 минут" });
        var messageSignal = await message.Task.WaitAsync(_timeout, CancellationToken);
        var notificationSignal = await notification.Task.WaitAsync(_timeout, CancellationToken);

        messageSignal.MessageId.Should().Be(messageId);
        messageSignal.SenderId.Should().Be(senderUser.UserId);
        notificationSignal.NotificationId.Should().NotBe(Guid.Empty);
    }
}

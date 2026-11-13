using AutoElite.Client.Authentication;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Client.Realtime;

/// <summary>
/// The connection of the signed-in user to the SignalR hub of the API, through which new notifications and chat
/// messages arrive at once. It carries the access token of the session, reconnects after a lost network, and only
/// listens: the client reads what an update is about from the API. Its events are raised on a thread of the
/// connection.
/// </summary>
public sealed class RealtimeConnection : IAsyncDisposable
{
    private const string HubPath = "hubs/realtime";

    private readonly Func<Uri> _apiAddress;
    private readonly AuthSessionManager _sessions;
    private readonly Action<HttpConnectionOptions>? _configure;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HubConnection? _connection;
    private Uri? _connectedAddress;

    /// <summary>Initializes a new instance of the <see cref="RealtimeConnection"/> class.</summary>
    /// <param name="apiAddress">Gives the current address of the API.</param>
    /// <param name="sessions">The session whose access token the connection carries.</param>
    /// <param name="configure">Configures the connection further, for example its transport in tests.</param>
    public RealtimeConnection(Func<Uri> apiAddress, AuthSessionManager sessions, Action<HttpConnectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(apiAddress);
        ArgumentNullException.ThrowIfNull(sessions);
        _apiAddress = apiAddress;
        _sessions = sessions;
        _configure = configure;
    }

    /// <summary>Occurs when the signed-in user receives a notification.</summary>
    public event EventHandler<NotificationSignal>? NotificationReceived;

    /// <summary>Occurs when a message arrives in a chat of the signed-in user.</summary>
    public event EventHandler<ChatMessageSignal>? ChatMessageReceived;

    /// <summary>
    /// Connects to the hub of the current API, unless already connected to it; a connection to another API, after the
    /// user changed the school, is replaced.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the connection is established.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var address = new Uri(_apiAddress(), HubPath);
            if (_connection is not null && _connectedAddress == address && _connection.State != HubConnectionState.Disconnected)
                return;

            await DisposeConnectionAsync();
            _connection = Build(address);
            _connectedAddress = address;
            await _connection.StartAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Disconnects from the hub, as when the user signs out.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await DisposeConnectionAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _lock.Dispose();
    }

    private HubConnection Build(Uri address)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(address, options =>
            {
                options.AccessTokenProvider = () => _sessions.GetAccessTokenAsync(CancellationToken.None);
                _configure?.Invoke(options);
            })
            .AddJsonProtocol(json => json.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ClientJsonContext.Default))
            .WithAutomaticReconnect()
            .Build();
        connection.On<NotificationSignal>("NotificationReceived", signal => NotificationReceived?.Invoke(this, signal));
        connection.On<ChatMessageSignal>("ChatMessageReceived", signal => ChatMessageReceived?.Invoke(this, signal));

        return connection;
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection is null)
            return;

        await _connection.DisposeAsync();
        _connection = null;
        _connectedAddress = null;
    }
}

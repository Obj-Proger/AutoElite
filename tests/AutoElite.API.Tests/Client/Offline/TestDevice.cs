using AutoElite.Client;
using AutoElite.Client.Authentication;
using AutoElite.Client.Offline;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.API.Tests.Client.Offline;

/// <summary>
/// A device with the client of the API on the test server and the offline data in a file of its own, whose network the
/// test turns off and on.
/// </summary>
internal sealed class TestDevice : IAsyncDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"autoelite-offline-{Guid.NewGuid():N}.db3");
    private volatile bool _isOnline = true;

    /// <summary>Initializes a new instance of the <see cref="TestDevice"/> class.</summary>
    /// <param name="api">The test server.</param>
    public TestDevice(ApiFactory api) =>
        Services = new ServiceCollection()
            .AddAutoEliteClient(
                api.Server.BaseAddress,
                http => http.ConfigurePrimaryHttpMessageHandler(() => new NetworkSwitch(this, api.Server.CreateHandler())))
            .AddSingleton<ISecretStore>(new MemorySecretStore())
            .AddAutoEliteOffline(_databasePath)
            .BuildServiceProvider();

    /// <summary>Gets or sets a value indicating whether the device reaches the network.</summary>
    public bool IsOnline
    {
        get => _isOnline;
        set => _isOnline = value;
    }

    /// <summary>Gets the services of the application on the device.</summary>
    public ServiceProvider Services { get; }

    /// <summary>Gets a service of the application.</summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <returns>The service.</returns>
    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    /// <summary>Signs the user in on the device, with the password of the test users.</summary>
    /// <param name="user">The user.</param>
    /// <returns>A task that completes when the user is signed in.</returns>
    public Task SignInAsync(InvitedUser user) =>
        Get<AuthSessionManager>().SignInAsync(user.Email, ApiFactory.Password, TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        File.Delete(_databasePath);
    }

    // Fails every request with the error of a network that cannot be reached while the device is offline.
    private sealed class NetworkSwitch(TestDevice device, HttpMessageHandler server) : DelegatingHandler(server)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            device.IsOnline
                ? base.SendAsync(request, cancellationToken)
                : Task.FromException<HttpResponseMessage>(new HttpRequestException("The device is offline."));
    }
}

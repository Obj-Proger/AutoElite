using System.Text.Json.Serialization;
using AutoElite.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.API.Realtime;

/// <summary>
/// The real-time channel of the API: <see cref="RealtimeHub"/> at <see cref="HubPath"/>, which sends the updates of
/// <see cref="IRealtimeUpdates"/> to the connected clients of each user.
/// </summary>
internal static class ApiRealtime
{
    /// <summary>The address of the hub.</summary>
    public const string HubPath = "/hubs/realtime";

    /// <summary>
    /// Adds SignalR with the hub's users named by the access tokens and enumerations as text, as in the API; the access
    /// token in the address of the hub; and the hub as the channel of the updates.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddApiRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(json => json.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IUserIdProvider, RealtimeUserIdProvider>();
        services.Replace(ServiceDescriptor.Singleton<IRealtimeUpdates, HubRealtimeUpdates>());

        // Browsers cannot send headers when they open a WebSocket or an event stream, so the clients of the hub send the
        // access token in the access_token parameter of its address; it is read there and nowhere else.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).PostConfigure(bearer =>
        {
            var next = bearer.Events.OnMessageReceived;
            bearer.Events.OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments(HubPath)
                    && context.Request.Query["access_token"].ToString() is { Length: > 0 } token)
                {
                    context.Token = token;
                }

                return next(context);
            };
        });

        return services;
    }

    /// <summary>Maps the hub for signed-in users.</summary>
    /// <param name="app">The host.</param>
    public static void MapApiRealtime(this WebApplication app) => app.MapHub<RealtimeHub>(HubPath);

    /// <summary>Names a connection by the user of its access token.</summary>
    private sealed class RealtimeUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst(AccessTokenClaims.UserId)?.Value;
    }
}

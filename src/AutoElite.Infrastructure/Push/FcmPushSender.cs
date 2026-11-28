using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AutoElite.Infrastructure.Push;

/// <summary>
/// Sends push notifications through the HTTP v1 API of Firebase Cloud Messaging, one message per device, with high
/// priority on Android and the channel of the application. A token the service no longer knows (404) is reported, so
/// that its device is removed; any other refusal is logged and the other devices are still tried.
/// </summary>
internal sealed partial class FcmPushSender(
    IHttpClientFactory httpClients,
    FcmAccessTokens accessTokens,
    FirebaseServiceAccount account,
    ILogger<FcmPushSender> logger) : IPushSender
{
    /// <summary>The notification channel the Android application creates for its notifications.</summary>
    public const string AndroidChannelId = "notifications";

    private static readonly JsonSerializerOptions _json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(message);
        if (tokens.Count == 0)
            return [];

        var accessToken = await accessTokens.GetAsync(cancellationToken);
        var endpoint = new Uri($"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(account.ProjectId)}/messages:send");
        var client = httpClients.CreateClient(FcmAccessTokens.HttpClientName);
        List<string> unknown = [];
        foreach (var token in tokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(BodyOf(token, message), options: _json)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                unknown.Add(token);
            else if (!response.IsSuccessStatusCode)
                LogRefused(logger, (int)response.StatusCode);
        }

        return unknown;
    }

    private static FcmRequest BodyOf(string token, PushMessage message) =>
        new(new FcmMessage(
            token,
            new FcmNotification(message.Title, message.Body),
            message.Data,
            new FcmAndroid("high", new FcmAndroidNotification(AndroidChannelId))));

    [LoggerMessage(Level = LogLevel.Warning, Message = "Firebase Cloud Messaging refused a push notification with status {StatusCode}.")]
    private static partial void LogRefused(ILogger logger, int statusCode);

    private sealed record FcmRequest([property: JsonPropertyName("message")] FcmMessage Message);

    private sealed record FcmMessage(
        [property: JsonPropertyName("token")] string Token,
        [property: JsonPropertyName("notification")] FcmNotification Notification,
        [property: JsonPropertyName("data")] IReadOnlyDictionary<string, string> Data,
        [property: JsonPropertyName("android")] FcmAndroid Android);

    private sealed record FcmNotification(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string? Body);

    private sealed record FcmAndroid(
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("notification")] FcmAndroidNotification Notification);

    private sealed record FcmAndroidNotification([property: JsonPropertyName("channel_id")] string ChannelId);
}

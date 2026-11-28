using System.Buffers.Text;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.Infrastructure.Push;

/// <summary>
/// The access tokens that let the service account send through Firebase Cloud Messaging: an assertion signed with the
/// private key of the account (a JWT with RS256) is exchanged at Google's token endpoint for a token that lasts an hour,
/// which is kept and renewed five minutes before it runs out, by one request at a time.
/// </summary>
internal sealed class FcmAccessTokens(IHttpClientFactory httpClients, FirebaseServiceAccount account, TimeProvider timeProvider)
    : IDisposable
{
    /// <summary>The name of the HTTP client of the token endpoint and of Firebase Cloud Messaging.</summary>
    public const string HttpClientName = "fcm";

    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";
    private static readonly TimeSpan _lifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan _renewalMargin = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _renewal = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <summary>Gives a valid access token, asking Google for a new one when the kept one is about to run out.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="HttpRequestException">Google refused the service account or did not answer.</exception>
    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (Valid() is { } token)
            return token;

        await _renewal.WaitAsync(cancellationToken);
        try
        {
            if (Valid() is { } renewed)
                return renewed;

            var now = timeProvider.GetUtcNow();
            using var content = new FormUrlEncodedContent(
            [
                new("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new("assertion", SignedAssertion(now))
            ]);
            using var response = await httpClients.CreateClient(HttpClientName)
                .PostAsync(account.TokenUri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Google refused the service account with status {(int)response.StatusCode}.", null, response.StatusCode);
            }

            var answer = await response.Content.ReadFromJsonAsync<TokenAnswer>(cancellationToken);
            if (answer is not { AccessToken.Length: > 0 })
                throw new HttpRequestException("Google answered without an access token.");

            _token = answer.AccessToken;
            _expiresAt = now.AddSeconds(answer.ExpiresIn > 0 ? answer.ExpiresIn : _lifetime.TotalSeconds);

            return _token;
        }
        finally
        {
            _renewal.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _renewal.Dispose();

    /// <summary>Signs the assertion of the service account: a JWT for the token endpoint with the scope of messaging.</summary>
    /// <param name="now">The current time.</param>
    /// <returns>The assertion.</returns>
    internal string SignedAssertion(DateTimeOffset now)
    {
        var header = Base64Url.EncodeToString("""{"alg":"RS256","typ":"JWT"}"""u8);
        var claims = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = account.ClientEmail,
            ["scope"] = Scope,
            ["aud"] = account.TokenUri.AbsoluteUri,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.Add(_lifetime).ToUnixTimeSeconds()
        });
        var unsigned = header + "." + Base64Url.EncodeToString(claims);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(account.PrivateKey);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return unsigned + "." + Base64Url.EncodeToString(signature);
    }

    private string? Valid() =>
        _token is { } token && timeProvider.GetUtcNow() < _expiresAt - _renewalMargin ? token : null;

    private sealed record TokenAnswer(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

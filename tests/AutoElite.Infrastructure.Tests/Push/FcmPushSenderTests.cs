using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoElite.Infrastructure.Push;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoElite.Infrastructure.Tests.Push;

public sealed class FcmPushSenderTests : IDisposable
{
    private static readonly Uri _tokenUri = new("https://oauth2.googleapis.com/token");

    private readonly RSA _key = RSA.Create(2048);
    private readonly FakeHttp _http = new();

    public void Dispose()
    {
        _key.Dispose();
        _http.Dispose();
    }

    private FirebaseServiceAccount Account() =>
        new("demo-project", "sender@demo-project.iam.gserviceaccount.com", _key.ExportPkcs8PrivateKeyPem(), _tokenUri);

    private string KeyJson(string tokenUri = "https://oauth2.googleapis.com/token") =>
        JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["type"] = "service_account",
            ["project_id"] = "demo-project",
            ["client_email"] = "sender@demo-project.iam.gserviceaccount.com",
            ["private_key"] = _key.ExportPkcs8PrivateKeyPem(),
            ["token_uri"] = tokenUri
        });

    [Fact]
    public void ProblemsOf_AKeyOfAServiceAccount_FindsNone()
    {
        FirebaseServiceAccount.ProblemsOf(KeyJson()).Should().BeEmpty();
        FirebaseServiceAccount.Parse(KeyJson()).ProjectId.Should().Be("demo-project");
    }

    [Fact]
    public void ProblemsOf_ATokenEndpointOtherThanGoogles_IsRefused()
    {
        FirebaseServiceAccount.ProblemsOf(KeyJson("https://attacker.example/token")).Should().ContainSingle()
            .Which.Should().Contain("token_uri");
    }

    [Fact]
    public void ProblemsOf_NotAKey_NamesWhatIsMissingWithoutTheSecrets()
    {
        FirebaseServiceAccount.ProblemsOf("""{"project_id":"demo"}""").Should().HaveCount(2);
        FirebaseServiceAccount.ProblemsOf("not json").Should().ContainSingle();
    }

    [Fact]
    public void SignedAssertion_IsAJwtOfTheAccountSignedWithItsKey()
    {
        var now = new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero);
        using var tokens = new FcmAccessTokens(_http, Account(), new FixedClock(now));

        var parts = tokens.SignedAssertion(now).Split('.');

        parts.Should().HaveCount(3);
        var signed = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
        _key.VerifyData(signed, Base64Url.DecodeFromChars(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .Should().BeTrue();
        using var claims = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        claims.RootElement.GetProperty("iss").GetString().Should().Be("sender@demo-project.iam.gserviceaccount.com");
        claims.RootElement.GetProperty("aud").GetString().Should().Be(_tokenUri.AbsoluteUri);
        claims.RootElement.GetProperty("scope").GetString().Should().Be("https://www.googleapis.com/auth/firebase.messaging");
        claims.RootElement.GetProperty("exp").GetInt64().Should().Be(now.AddHours(1).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task SendAsync_SendsToEachDeviceWithOneAccessToken_AndReportsTheUnknownOnes()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        using var tokens = new FcmAccessTokens(_http, Account(), clock);
        var sender = new FcmPushSender(_http, tokens, Account(), NullLogger<FcmPushSender>.Instance);
        var message = new PushMessage("New message", "Hello", new Dictionary<string, string> { ["type"] = "NewMessage" });

        var unknown = await sender.SendAsync(["device-1", "gone", "device-2"], message, TestContext.Current.CancellationToken);

        unknown.Should().Equal("gone");
        _http.Requests.Count(request => request.Uri == _tokenUri).Should().Be(1);
        var sends = _http.Requests.Where(request => request.Uri.Host == "fcm.googleapis.com").ToList();
        sends.Should().HaveCount(3);
        sends.Should().OnlyContain(request => request.Authorization == "Bearer access-1"
            && request.Uri.AbsolutePath == "/v1/projects/demo-project/messages:send");
        using var body = JsonDocument.Parse(sends[0].Body);
        var sent = body.RootElement.GetProperty("message");
        sent.GetProperty("token").GetString().Should().Be("device-1");
        sent.GetProperty("notification").GetProperty("title").GetString().Should().Be("New message");
        sent.GetProperty("data").GetProperty("type").GetString().Should().Be("NewMessage");
        sent.GetProperty("android").GetProperty("notification").GetProperty("channel_id").GetString()
            .Should().Be(FcmPushSender.AndroidChannelId);
    }

    [Fact]
    public void AddInfrastructure_WithoutAServiceAccount_SendsNoPush()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=autoelite"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(TestCurrentUser.System);
        services.AddSingleton(TimeProvider.System);

        using var provider = services.AddInfrastructure(configuration).BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<IPushSender>().Should().BeOfType<NoPushSender>();
    }

    private sealed record SentRequest(Uri Uri, string? Authorization, string Body);

    // Answers the token endpoint with an access token and Firebase Cloud Messaging with 404 for the token "gone".
    private sealed class FakeHttp : HttpMessageHandler, IHttpClientFactory
    {
        public List<SentRequest> Requests { get; } = [];

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new SentRequest(request.RequestUri!, request.Headers.Authorization?.ToString(), body));
            if (request.RequestUri == _tokenUri)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"access_token":"access-1","expires_in":3600,"token_type":"Bearer"}""",
                        Encoding.UTF8, "application/json")
                };
            }

            var isGone = body.Contains("\"gone\"", StringComparison.Ordinal);

            return new HttpResponseMessage(isGone ? HttpStatusCode.NotFound : HttpStatusCode.OK);
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

[Collection(nameof(ApiCollection))]
public sealed class AuthControllerTests(ApiFactory api)
{
    private const string Password = "Correct-horse-7";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<(InvitedUser User, AuthTokens Tokens)> SignInAsync(HttpClient client)
    {
        var user = await api.InviteAsync(UserRole.Student);
        using var accepted = await client.PostAsJsonAsync(
            "/api/v1/auth/invitation", new { user.UserId, Token = user.InvitationToken, Password }, CancellationToken);
        accepted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { user.Email, Password }, CancellationToken);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>(ApiJson.Options, CancellationToken);

        return (user, tokens!);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string uri, AuthTokens tokens) =>
        new(method, uri) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken.Value) } };

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));

        return problem.RootElement.Clone();
    }

    [Fact]
    public async Task InvitedUser_AcceptsSignsInReadsTheProfileAndRenewsTheSession()
    {
        using var client = api.CreateClient();
        var (user, tokens) = await SignInAsync(client);

        using var profileRequest = Authorized(HttpMethod.Get, "/api/v1/me", tokens);
        using var profileResponse = await client.SendAsync(profileRequest, CancellationToken);
        using var renewed = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { RefreshToken = tokens.RefreshToken.Value }, CancellationToken);

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfile>(ApiJson.Options, CancellationToken);
        profile!.Id.Should().Be(user.UserId);
        profile.Email.Should().Be(user.Email);
        profile.Roles.Should().Equal(UserRole.Student);
        renewed.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await renewed.Content.ReadFromJsonAsync<AuthTokens>(ApiJson.Options, CancellationToken);
        newTokens!.RefreshToken.Value.Should().NotBe(tokens.RefreshToken.Value);
    }

    [Fact]
    public async Task RefreshTokenUsedTwice_IsRefused()
    {
        using var client = api.CreateClient();
        var (_, tokens) = await SignInAsync(client);
        var refresh = new { RefreshToken = tokens.RefreshToken.Value };
        using var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", refresh, CancellationToken);

        using var second = await client.PostAsJsonAsync("/api/v1/auth/refresh", refresh, CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ProblemAsync(second)).GetProperty("code").GetString().Should().Be("Account.RefreshTokenInvalid");
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        using var client = api.CreateClient();
        var (_, tokens) = await SignInAsync(client);
        var refresh = new { RefreshToken = tokens.RefreshToken.Value };

        using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", refresh, CancellationToken);
        using var renewed = await client.PostAsJsonAsync("/api/v1/auth/refresh", refresh, CancellationToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        renewed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ChangesWhatTheProfileShows()
    {
        using var client = api.CreateClient();
        var (_, tokens) = await SignInAsync(client);
        using var update = Authorized(HttpMethod.Put, "/api/v1/me", tokens);
        update.Content = JsonContent.Create(
            new { LastName = "Петрова", FirstName = "Анна", PreferredLanguage = Language.Russian }, options: ApiJson.Options);

        using var updated = await client.SendAsync(update, CancellationToken);
        using var read = Authorized(HttpMethod.Get, "/api/v1/me", tokens);
        using var profileResponse = await client.SendAsync(read, CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfile>(ApiJson.Options, CancellationToken);
        profile!.LastName.Should().Be("Петрова");
        profile.FirstName.Should().Be("Анна");
        profile.PreferredLanguage.Should().Be(Language.Russian);
    }

    [Fact]
    public async Task Profile_WithoutToken_IsUnauthorized_InTheLanguageOfTheRequest()
    {
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me") { Headers = { { "Accept-Language", "ru-RU" } } };

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("Auth.Unauthenticated");
        problem.GetProperty("detail").GetString().Should().Be("Необходимо войти в систему.");
    }

    [Fact]
    public async Task Login_WrongPassword_IsUnauthorized()
    {
        using var client = api.CreateClient();
        var (user, _) = await SignInAsync(client);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { user.Email, Password = "Wrong-password-1" }, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ProblemAsync(response)).GetProperty("code").GetString().Should().Be("Account.InvalidCredentials");
    }

    [Fact]
    public async Task Login_WithoutEmailAndPassword_NamesBothFields()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { }, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("Validation.Failed");
        problem.GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("field").GetString())
            .Should().Contain("email").And.Contain("password");
    }

    [Fact]
    public async Task Login_MalformedJson_IsAValidationError()
    {
        using var client = api.CreateClient();
        using var content = new StringContent("{ \"email\": ", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/auth/login", content, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemAsync(response)).GetProperty("code").GetString().Should().Be("Validation.Failed");
    }
}

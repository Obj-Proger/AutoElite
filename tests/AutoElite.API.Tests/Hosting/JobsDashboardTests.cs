using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Hosting;

/// <summary>The dashboard of the background jobs, which only administrators open.</summary>
[Collection(nameof(ApiCollection))]
public sealed class JobsDashboardTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Administrator_OpensASessionAndTheDashboard()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);

        using var withoutSession = await admin.GetAsync("/jobs", CancellationToken);
        using var session = await admin.PostAsync("/api/v1/jobs-dashboard/session", null, CancellationToken);
        using var dashboard = await admin.GetAsync("/jobs", CancellationToken);

        withoutSession.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        session.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cookie = session.Headers.GetValues("Set-Cookie").Should().ContainSingle().Which.ToUpperInvariant();
        cookie.Should().Contain("PATH=/JOBS").And.Contain("HTTPONLY").And.Contain("SAMESITE=STRICT");
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);
        dashboard.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Student_CannotOpenASession()
    {
        using var student = await api.SignedInClientAsync(UserRole.Student);

        using var session = await student.PostAsync("/api/v1/jobs-dashboard/session", null, CancellationToken);

        session.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await session.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task Dashboard_WithoutASession_IsRefused()
    {
        using var anonymous = api.CreateClient();

        using var dashboard = await anonymous.GetAsync("/jobs", CancellationToken);

        dashboard.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

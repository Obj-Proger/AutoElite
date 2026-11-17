using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutoElite.API.DemoData;
using AutoElite.Application.Users.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.API.Tests.Hosting;

/// <summary>
/// The demo data of development: every step is a command of the application on behalf of a demo user, the past ones
/// with the demo clock turned back, so a step the rules refuse would leave the data incomplete; the test runs the
/// seeder on the test server and signs in as the demo users.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class DemoDataTests(ApiFactory api)
{
    private const string Password = "Demo-password-1";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<HttpClient> SignInAsync(string name)
    {
        var client = api.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { Email = $"{name}@{DemoDataSeeder.EmailDomain}", Password }, CancellationToken);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>(ApiJson.Options, CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken.Value);

        return client;
    }

    private static string Period(int fromDays, int toDays)
    {
        var now = DateTime.UtcNow;

        return $"fromUtc={Uri.EscapeDataString(now.AddDays(fromDays).ToString("O", CultureInfo.InvariantCulture))}"
            + $"&toUtc={Uri.EscapeDataString(now.AddDays(toDays).ToString("O", CultureInfo.InvariantCulture))}";
    }

    [Fact]
    public async Task DemoData_IsCreatedOnce_AndTheStudentsSeeTheirLessons()
    {
        var logger = new RecordingLogger();
        var seeder = new DemoDataSeeder(
            api.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DemoDataOptions { Enabled = true, Password = Password }),
            api.Services.GetRequiredService<DemoClock>(),
            logger);

        await seeder.StartAsync(CancellationToken);
        await seeder.StartAsync(CancellationToken);

        logger.Errors.Should().BeEmpty();
        api.Services.GetRequiredService<DemoClock>().GetUtcNow().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        using var first = await SignInAsync("student1");
        using var third = await SignInAsync("student3");
        using var driving = await SignInAsync("driving");
        using var manager = await SignInAsync("manager");
        (await first.GetJsonAsync($"/api/v1/me/driving-lessons?{Period(0, 60)}")).GetArrayLength().Should().Be(3);
        (await third.GetJsonAsync($"/api/v1/me/driving-lessons?{Period(0, 60)}")).GetArrayLength().Should().Be(1);
        (await first.GetJsonAsync($"/api/v1/me/theory-lessons?{Period(0, 60)}")).GetArrayLength().Should().Be(9);
        (await first.GetJsonAsync($"/api/v1/me/driving-lessons?{Period(-30, 0)}")).EnumerateArray()
            .Should().Contain(lesson => lesson.GetProperty("booking").GetProperty("status").GetString() == "Completed");
        (await first.GetJsonAsync("/api/v1/me/exams")).GetArrayLength().Should().Be(3);
        (await driving.GetJsonAsync("/api/v1/me/staff-tasks?status=New")).GetProperty("items").GetArrayLength().Should().Be(2);
        var newLead = await manager.GetJsonAsync($"/api/v1/leads?status=New&search={Uri.EscapeDataString("Ковалёва")}");
        newLead.GetProperty("items").GetArrayLength().Should().Be(1);
        var lostLead = await manager.GetJsonAsync($"/api/v1/leads?status=Lost&search={Uri.EscapeDataString("Белов Игорь")}");
        lostLead.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    // Keeps the errors the seeder logs, which tell the step that failed.
    private sealed class RecordingLogger : ILogger<DemoDataSeeder>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                Errors.Add(formatter(state, exception));
        }
    }
}

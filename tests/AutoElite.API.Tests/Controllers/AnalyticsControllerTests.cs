using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>The figures and the reports of the school, and the card of a student.</summary>
[Collection(nameof(ApiCollection))]
public sealed class AnalyticsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A few days around today, so that today of the school's time zone is inside.
    private static string Period()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return string.Create(
            CultureInfo.InvariantCulture, $"from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}");
    }

    [Fact]
    public async Task Administrator_ReadsTheFigures()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        await admin.CreateAsync("/api/v1/leads", new { ContactName = "Пётр Орлов", Phone = "+79990003344", Source = LeadSource.Referral });
        var period = Period();

        var leads = await admin.GetJsonAsync($"/api/v1/analytics/leads?{period}");
        var revenue = await admin.GetJsonAsync($"/api/v1/analytics/revenue?{period}");
        var contracts = await admin.GetJsonAsync($"/api/v1/analytics/contracts?{period}");
        var workload = await admin.GetJsonAsync($"/api/v1/analytics/instructor-workload?{period}");
        var activity = await admin.GetJsonAsync($"/api/v1/analytics/staff-activity?{period}");

        leads.GetProperty("total").GetInt32().Should().BePositive();
        leads.GetProperty("bySource").EnumerateArray().Select(item => item.GetProperty("source").GetString())
            .Should().Contain(nameof(LeadSource.Referral));
        revenue.ValueKind.Should().Be(JsonValueKind.Array);
        contracts.TryGetProperty("active", out _).Should().BeTrue();
        workload.ValueKind.Should().Be(JsonValueKind.Array);
        activity.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Theory]
    [InlineData("Pdf", "application/pdf", "%PDF-")]
    [InlineData("Excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PK")]
    public async Task Administrator_DownloadsAReport(string format, string contentType, string signature)
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);

        using var response = await admin.GetAsync($"/api/v1/analytics/reports/Revenue?{Period()}&format={format}", CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(contentType);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        content.Should().StartWith(Encoding.ASCII.GetBytes(signature));
    }

    [Fact]
    public async Task Student_ReadsTheirOwnCard_ButNotTheFigures()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var courseId = await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = studentUser.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _contractStart,
            EndDate = _contractStart.AddMonths(6)
        });

        var card = await student.GetJsonAsync($"/api/v1/students/{studentUser.UserId}/card");
        using var figures = await student.GetAsync($"/api/v1/analytics/revenue?{Period()}", CancellationToken);

        card.GetProperty("studentId").GetGuid().Should().Be(studentUser.UserId);
        var contract = card.GetProperty("contracts").EnumerateArray().Should().ContainSingle().Which;
        contract.GetProperty("contract").GetProperty("id").GetGuid().Should().Be(contractId);
        contract.GetProperty("contract").GetProperty("debtAmount").GetDecimal().Should().Be(45000m);
        figures.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

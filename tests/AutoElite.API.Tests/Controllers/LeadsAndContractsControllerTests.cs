using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>A lead from the public form to the contract of its student.</summary>
[Collection(nameof(ApiCollection))]
public sealed class LeadsAndContractsControllerTests(ApiFactory api)
{
    // A start in the past, so that a contract can be closed today.
    private static readonly DateOnly _start = new(2026, 10, 1);

    private static async Task<Guid> CreateCourseAsync(HttpClient admin) =>
        await admin.CreateAsync("/api/v1/courses", new
        {
            Name = $"Категория B {Guid.NewGuid():N}",
            Category = LicenseCategory.B,
            PriceAmount = 45000m,
            TheoryHours = 134,
            PracticeHours = 56
        });

    [Fact]
    public async Task PublicRequest_IsAcceptedWithoutSigningIn()
    {
        using var anonymous = api.CreateClient();

        using var response = await anonymous.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/leads/requests",
            new { ContactName = "Анна Петрова", Phone = "+79990001122", DesiredCategory = LicenseCategory.B });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Manager_WorksALeadAndConvertsItIntoTheContractOfANewStudent()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var courseId = await CreateCourseAsync(admin);
        var leadId = await admin.CreateAsync(
            "/api/v1/leads", new { ContactName = "Иван Сидоров", Phone = "+79990002233", Source = LeadSource.PhoneCall });
        var studentEmail = $"{Guid.NewGuid():N}@example.com";

        using var started = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/start-work");
        var noteId = await admin.CreateAsync($"/api/v1/leads/{leadId}/notes", new { Text = "Перезвонить вечером" });
        using var converted = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/conversion", new
        {
            LastName = "Сидоров",
            FirstName = "Иван",
            Email = studentEmail,
            PreferredLanguage = Language.Russian,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _start,
            EndDate = _start.AddMonths(3)
        });
        var conversion = await converted.ReadJsonAsync();
        var contractId = conversion.GetProperty("contractId").GetGuid();
        var lead = await admin.GetJsonAsync($"/api/v1/leads/{leadId}");

        started.StatusCode.Should().Be(HttpStatusCode.NoContent);
        converted.StatusCode.Should().Be(HttpStatusCode.Created);
        converted.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/contracts/{contractId}");
        lead.GetProperty("status").GetString().Should().Be(nameof(LeadStatus.Converted));
        lead.GetProperty("contractId").GetGuid().Should().Be(contractId);
        lead.GetProperty("notes").EnumerateArray().Select(note => note.GetProperty("id").GetGuid()).Should().Equal(noteId);

        using var student = await api.SignInRegisteredAsync(conversion.GetProperty("studentId").GetGuid(), studentEmail);
        var own = await student.GetJsonAsync("/api/v1/me/contracts");
        var contract = await student.GetJsonAsync($"/api/v1/contracts/{contractId}");

        own.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(contractId);
        contract.GetProperty("courseId").GetGuid().Should().Be(courseId);
        contract.GetProperty("priceAmount").GetDecimal().Should().Be(45000m);
        contract.GetProperty("status").GetString().Should().Be(nameof(ContractStatus.Active));
    }

    [Fact]
    public async Task LostLead_IsReopened()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var leadId = await admin.CreateAsync(
            "/api/v1/leads", new { ContactName = "Ольга Ким", Email = "olga.kim@example.com", Source = LeadSource.Website });

        using var lost = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/lost", new { Reason = "Дорого" });
        var lostLead = await admin.GetJsonAsync($"/api/v1/leads/{leadId}");
        using var reopened = await admin.SendJsonAsync(HttpMethod.Post, $"/api/v1/leads/{leadId}/reopen");
        var reopenedLead = await admin.GetJsonAsync($"/api/v1/leads/{leadId}");

        lost.StatusCode.Should().Be(HttpStatusCode.NoContent);
        lostLead.GetProperty("lostReason").GetString().Should().Be("Дорого");
        reopened.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reopenedLead.GetProperty("status").GetString().Should().NotBe(nameof(LeadStatus.Lost));
    }

    [Fact]
    public async Task Manager_ChangesTheTermsOfAContractAndTerminatesIt()
    {
        using var admin = await api.SignedInClientAsync(UserRole.Administrator);
        var student = await api.InviteAsync(UserRole.Student);
        var courseId = await CreateCourseAsync(admin);
        var contractId = await admin.CreateAsync("/api/v1/contracts", new
        {
            StudentId = student.UserId,
            CourseId = courseId,
            Transmission = Transmission.Manual,
            StartDate = _start,
            EndDate = _start.AddMonths(3)
        });
        var contracts = $"/api/v1/contracts/{contractId}";

        using var priced = await admin.SendJsonAsync(HttpMethod.Put, $"{contracts}/price", new { PriceAmount = 40000m });
        using var automatic = await admin.SendJsonAsync(
            HttpMethod.Put, $"{contracts}/transmission", new { Transmission = Transmission.Automatic });
        using var moved = await admin.SendJsonAsync(
            HttpMethod.Put, $"{contracts}/dates", new { StartDate = _start, EndDate = _start.AddMonths(4) });
        using var terminated = await admin.SendJsonAsync(
            HttpMethod.Post, $"{contracts}/terminate", new { Reason = "Переезд", TerminatedOn = DateOnly.FromDateTime(DateTime.UtcNow) });
        var contract = await admin.GetJsonAsync(contracts);
        var found = await admin.GetJsonAsync($"/api/v1/contracts?studentId={student.UserId}&status=Terminated");

        priced.StatusCode.Should().Be(HttpStatusCode.NoContent);
        automatic.StatusCode.Should().Be(HttpStatusCode.NoContent);
        moved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        terminated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        contract.GetProperty("priceAmount").GetDecimal().Should().Be(40000m);
        contract.GetProperty("transmission").GetString().Should().Be(nameof(Transmission.Automatic));
        contract.GetProperty("endDate").GetString().Should().Be("2027-02-01");
        contract.GetProperty("terminationReason").GetString().Should().Be("Переезд");
        found.GetProperty("totalCount").GetInt32().Should().Be(1);
    }
}

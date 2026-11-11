using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>Files attached to records: a scan of a contract and a profile photo.</summary>
[Collection(nameof(ApiCollection))]
public sealed class DocumentsControllerTests(ApiFactory api)
{
    private static readonly DateOnly _contractStart = new(2026, 10, 1);

    private static readonly byte[] _pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% a scan of the tests\n%%EOF\n");

    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpResponseMessage> AttachAsync(
        HttpClient client, byte[] content, string fileName, string contentType, EntityKind ownerKind, Guid ownerId)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(Enum.GetName(ownerKind)!), "ownerKind");
        form.Add(new StringContent(ownerId.ToString()), "ownerId");
        form.Add(new StringContent("Скан договора"), "description");

        return await client.PostAsync("/api/v1/documents", form, CancellationToken);
    }

    [Fact]
    public async Task Manager_AttachesAScanToAContract_WhichItsStudentDownloads()
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

        using var attached = await AttachAsync(admin, _pdf, "договор.pdf", "application/pdf", EntityKind.Contract, contractId);
        var documentId = (await attached.ReadJsonAsync()).GetProperty("id").GetGuid();
        var document = $"/api/v1/documents/{documentId}";
        var listed = await student.GetJsonAsync($"/api/v1/documents?ownerKind=Contract&ownerId={contractId}");
        using var downloaded = await student.GetAsync(document, CancellationToken);
        var content = await downloaded.Content.ReadAsByteArrayAsync(CancellationToken);
        using var described = await admin.SendJsonAsync(HttpMethod.Put, $"{document}/description", new { Description = "Договор" });
        using var deleted = await admin.DeleteAsync(document, CancellationToken);
        using var gone = await student.GetAsync(document, CancellationToken);

        attached.StatusCode.Should().Be(HttpStatusCode.Created);
        attached.Headers.Location!.AbsolutePath.Should().Be(document);
        var item = listed.EnumerateArray().Should().ContainSingle().Which;
        item.GetProperty("id").GetGuid().Should().Be(documentId);
        item.GetProperty("description").GetString().Should().Be("Скан договора");
        downloaded.StatusCode.Should().Be(HttpStatusCode.OK);
        downloaded.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        downloaded.Content.Headers.ContentDisposition!.FileNameStar.Should().Be("договор.pdf");
        downloaded.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        content.Should().Equal(_pdf);
        described.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task User_MakesAnAttachedImageTheirPhoto_WhichOthersCanDownload()
    {
        var (user, signedInUser) = await api.SignInAsync(UserRole.Student);
        using var userClient = user;
        using var other = await api.SignedInClientAsync(UserRole.TheoryInstructor);

        using var attached = await AttachAsync(user, _png, "photo.png", "image/png", EntityKind.User, signedInUser.UserId);
        var documentId = (await attached.ReadJsonAsync()).GetProperty("id").GetGuid();
        using var set = await user.PutAsJsonAsync("/api/v1/me/photo", new { DocumentId = documentId }, CancellationToken);
        var profile = await user.GetJsonAsync("/api/v1/me");
        using var downloaded = await other.GetAsync($"/api/v1/documents/{documentId}", CancellationToken);

        attached.StatusCode.Should().Be(HttpStatusCode.Created);
        set.StatusCode.Should().Be(HttpStatusCode.NoContent);
        profile.GetProperty("photoDocumentId").GetGuid().Should().Be(documentId);
        downloaded.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloaded.Content.ReadAsByteArrayAsync(CancellationToken)).Should().Equal(_png);
    }
}

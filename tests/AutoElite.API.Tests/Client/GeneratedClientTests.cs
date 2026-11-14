using System.Globalization;
using AutoElite.Client;
using AutoElite.Domain.Enums;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using InviteUser = AutoElite.Client.Api.Models.InviteUserCommand;
using Problem = AutoElite.Client.Api.Models.ProblemDetails;
using Role = AutoElite.Client.Api.Models.UserRole;
using SetPhoto = AutoElite.Client.Api.Models.SetMyPhotoCommand;
using LeadRequest = AutoElite.Client.Api.Models.SubmitLeadRequestCommand;

namespace AutoElite.API.Tests.Client;

/// <summary>The client of the API that AutoElite.Client generates from the OpenAPI document of the build.</summary>
[Collection(nameof(ApiCollection))]
public sealed class GeneratedClientTests(ApiFactory api)
{
    // The signature of a PNG image and the start of its header, which the API takes for an image.
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Visitor_SendsARequestThroughTheClient_WhichTheManagerFinds()
    {
        using var anonymous = api.CreateClient();
        using var manager = await api.SignedInClientAsync(UserRole.Administrator);
        var phone = "+7998" + Random.Shared.Next(1_000_000, 9_999_999).ToString(CultureInfo.InvariantCulture);
        var client = AutoEliteApiClientFactory.Create(anonymous);

        await client.Api.V1.Leads.Requests.PostAsync(
            new LeadRequest { ContactName = "Олег Смирнов", Phone = phone }, cancellationToken: CancellationToken);
        var leads = await manager.GetJsonAsync($"/api/v1/leads?search={Uri.EscapeDataString(phone)}");

        leads.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Administrator_ReadsTheBranchesThroughTheClient()
    {
        using var administrator = await api.SignedInClientAsync(UserRole.Administrator);
        var client = AutoEliteApiClientFactory.Create(administrator);

        var branches = await client.Api.V1.Branches.GetAsync(cancellationToken: CancellationToken);

        branches.Should().NotBeNull();
    }

    [Fact]
    public async Task Request_WithoutSigningIn_FailsWithTheStatusOfTheProblem()
    {
        using var anonymous = api.CreateClient();
        var client = AutoEliteApiClientFactory.Create(anonymous);

        var reading = () => client.Api.V1.Branches.GetAsync(cancellationToken: CancellationToken);

        (await reading.Should().ThrowAsync<ApiException>()).Which.ResponseStatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvalidRequest_FailsWithTheProblem_AndTheMessagesOfItsFields()
    {
        using var administrator = await api.SignedInClientAsync(UserRole.Administrator);
        var client = AutoEliteApiClientFactory.Create(administrator);

        var inviting = () => client.Api.V1.Users.PostAsync(
            new InviteUser { LastName = "Смирнов", FirstName = "Олег", Email = "not-an-email", Roles = [Role.Student] },
            cancellationToken: CancellationToken);

        var problem = (await inviting.Should().ThrowAsync<Problem>()).Which;
        problem.ResponseStatusCode.Should().Be(400);
        problem.Detail.Should().NotBeNullOrEmpty();
        var fields = problem.AdditionalData["errors"].Should().BeOfType<UntypedArray>().Which.GetValue()
            .Select(error => error.Should().BeOfType<UntypedObject>().Which.GetValue())
            .Select(error => (Field: ((UntypedString)error["field"]).GetValue(), Message: ((UntypedString)error["message"]).GetValue()))
            .ToList();
        fields.Should().Contain(error => error.Field == "email" && !string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public async Task User_AttachesAPhotoThroughTheClient_AndMakesItTheirPhoto()
    {
        var (http, user) = await api.SignInAsync(UserRole.Student);
        using var signedIn = http;
        var client = AutoEliteApiClientFactory.Create(signedIn);
        using var image = new MemoryStream(_png);
        var form = new MultipartBody();
        form.AddOrReplacePart("file", "image/png", image, "photo.png");
        form.AddOrReplacePart("ownerKind", "text/plain", nameof(EntityKind.User));
        form.AddOrReplacePart("ownerId", "text/plain", user.UserId.ToString("D", CultureInfo.InvariantCulture));

        var document = await client.AttachDocumentAsync(form, CancellationToken);
        await client.Api.V1.Me.Photo.PutAsync(new SetPhoto { DocumentId = document.Id }, cancellationToken: CancellationToken);
        var profile = await client.Api.V1.Me.GetAsync(cancellationToken: CancellationToken);

        profile!.PhotoDocumentId.Should().Be(document.Id);
    }
}

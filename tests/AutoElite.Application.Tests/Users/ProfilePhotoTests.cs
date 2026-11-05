using AutoElite.Application.Users.RemoveMyPhoto;
using AutoElite.Application.Users.SetMyPhoto;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class ProfilePhotoTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeDocumentRepository _documents = new();
    private readonly User _user = TestData.NewUser("student@example.com");
    private readonly FakeCurrentUser _caller;

    public ProfilePhotoTests()
    {
        _users.Seed(_user);
        _caller = new FakeCurrentUser { UserId = _user.Id, Roles = new HashSet<UserRole> { UserRole.Student } };
    }

    private Document SeedDocument(Guid ownerId, string fileName = "me.png", string contentType = "image/png")
    {
        var document = Document.Attach(
                EntityReference.Create(EntityKind.User, ownerId).Value, fileName, contentType, 10,
                $"documents/{Guid.CreateVersion7():N}", null, ownerId, TestData.Now)
            .Value;
        _documents.Seed(document);

        return document;
    }

    private Task<Result> SetPhoto(Document document) =>
        new SetMyPhotoCommandHandler(_users, _documents, _caller)
            .Handle(new SetMyPhotoCommand(document.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task SetPhoto_OwnImage_SetsIt()
    {
        var photo = SeedDocument(_user.Id);

        var result = await SetPhoto(photo);

        result.IsSuccess.Should().BeTrue();
        _user.PhotoDocumentId.Should().Be(photo.Id);
    }

    [Fact]
    public async Task SetPhoto_ImageOfAnotherUser_ReturnsDocumentNotFound()
    {
        var photo = SeedDocument(Guid.CreateVersion7());

        var result = await SetPhoto(photo);

        result.Error.Should().Be(ApplicationErrors.NotFound<Document>());
        _user.PhotoDocumentId.Should().BeNull();
    }

    [Fact]
    public async Task SetPhoto_Pdf_ReturnsImageRequired()
    {
        var scan = SeedDocument(_user.Id, "passport.pdf", "application/pdf");

        var result = await SetPhoto(scan);

        result.Error.Should().Be(ApplicationErrors.Document.ImageRequired);
    }

    [Fact]
    public async Task RemovePhoto_UserWithPhoto_ClearsIt()
    {
        _user.SetPhoto(SeedDocument(_user.Id).Id).IsSuccess.Should().BeTrue();

        var result = await new RemoveMyPhotoCommandHandler(_users, _caller)
            .Handle(new RemoveMyPhotoCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _user.PhotoDocumentId.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DocumentDeleted_ClearsThePhotoOnlyIfItWasThePhoto(bool deletedIsThePhoto)
    {
        var photo = SeedDocument(_user.Id);
        var other = SeedDocument(_user.Id, "passport.pdf", "application/pdf");
        _user.SetPhoto(photo.Id).IsSuccess.Should().BeTrue();
        var deleted = deletedIsThePhoto ? photo : other;
        deleted.Delete(TestData.Now).IsSuccess.Should().BeTrue();

        await new RemovePhotoWhenDocumentDeletedHandler(_users).Handle(
            deleted.DomainEvents.OfType<DocumentDeletedDomainEvent>().Single(), TestContext.Current.CancellationToken);

        _user.PhotoDocumentId.Should().Be(deletedIsThePhoto ? (Guid?)null : photo.Id);
    }
}

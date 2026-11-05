using AutoElite.Application.Documents.AttachDocument;
using AutoElite.Application.Documents.DeleteDocument;
using AutoElite.Application.Documents.GetDocumentFile;
using AutoElite.Application.Documents.GetDocuments;
using AutoElite.Application.Documents.UpdateDocumentDescription;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Documents;

public class DocumentHandlerTests
{
    private static readonly byte[] _pdf = [0x25, 0x50, 0x44, 0x46];

    private readonly DocumentFixture _fixture = new();
    private readonly FakeCurrentUser _manager = DocumentFixture.Caller(Guid.CreateVersion7(), UserRole.Manager, DocumentFixture.BranchId);
    private readonly User _student = TestData.NewUser("student@example.com");

    private async Task<Result<Guid>> Attach(EntityKind kind, Guid ownerId, FakeCurrentUser caller, string fileName = "scan.pdf")
    {
        await using var content = new MemoryStream(_pdf);
        var command = new AttachDocumentCommand(kind, ownerId, fileName, "application/pdf", _pdf.Length, "Passport", content);

        return await new AttachDocumentCommandHandler(
                _fixture.Documents, _fixture.CreateOwners(caller), _fixture.Storage, caller, TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);
    }

    private Task<Result> Delete(Document document, FakeCurrentUser caller) =>
        new DeleteDocumentCommandHandler(_fixture.CreateAccess(caller), _fixture.Documents, _fixture.Chats, TestData.Clock())
            .Handle(new DeleteDocumentCommand(document.Id), TestContext.Current.CancellationToken);

    private FakeCurrentUser StudentCaller() => DocumentFixture.Caller(_student.Id, UserRole.Student);

    [Fact]
    public async Task Attach_ManagerToAContractOfTheBranch_StoresTheFileAndRecordsTheDocument()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);

        var result = await Attach(EntityKind.Contract, contract.Id, _manager);

        var document = _fixture.Documents.Items.Should().ContainSingle().Which;
        document.Id.Should().Be(result.Value);
        document.Owner.Should().Be(EntityReference.Create(EntityKind.Contract, contract.Id).Value);
        document.UploadedByUserId.Should().Be(_manager.UserId!.Value);
        _fixture.Storage.Files[document.StorageKey].Should().Equal(_pdf);
    }

    [Fact]
    public async Task Attach_StudentToTheirOwnContract_ReturnsForbiddenBecauseStudentsOnlyReadContracts()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);

        var result = await Attach(EntityKind.Contract, contract.Id, StudentCaller());

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _fixture.Storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Attach_ManagerToAContractOfAnotherBranch_ReturnsOwnerNotFound()
    {
        var contract = _fixture.SeedContract(_student, Guid.CreateVersion7());

        var result = await Attach(EntityKind.Contract, contract.Id, _manager);

        result.Error.Should().Be(ApplicationErrors.Document.OwnerNotFound);
    }

    [Fact]
    public async Task Attach_StudentToTheirOwnProfile_RecordsTheDocument()
    {
        _fixture.Users.Seed(_student);

        var result = await Attach(EntityKind.User, _student.Id, StudentCaller());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Attach_ToAKindThatTakesNoDocuments_ReturnsOwnerKindNotSupported()
    {
        var result = await Attach(EntityKind.Payment, Guid.CreateVersion7(), _manager);

        result.Error.Should().Be(ApplicationErrors.Document.OwnerKindNotSupported);
    }

    [Fact]
    public async Task Attach_FormatNotAllowed_ReturnsTheDomainErrorAndStoresNothing()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);

        var result = await Attach(EntityKind.Contract, contract.Id, _manager, fileName: "virus.exe");

        result.Error.Should().Be(DomainErrors.Document.FormatNotAllowed);
        _fixture.Storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Attach_StorageRejectsTheContent_RecordsNothing()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);
        _fixture.Storage.RejectSignature = true;

        var result = await Attach(EntityKind.Contract, contract.Id, _manager);

        result.Error.Should().Be(ApplicationErrors.Document.ContentMismatch);
        _fixture.Documents.Items.Should().BeEmpty();
    }

    [Fact]
    public void AttachValidator_NoContent_ReportsContentMissing()
    {
        var command = new AttachDocumentCommand(EntityKind.Contract, Guid.CreateVersion7(), "scan.pdf", "application/pdf", 3, null, null);

        var result = new AttachDocumentCommandValidator().Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Document.ContentMissing.Code);
    }

    [Fact]
    public async Task Delete_OwnAttachmentInAChat_RemovesTheRecordAndRaisesTheEvent()
    {
        var chat = _fixture.SeedChat(_student.Id, Guid.CreateVersion7());
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Chat, chat.Id).Value, _student.Id);

        var result = await Delete(document, StudentCaller());

        result.IsSuccess.Should().BeTrue();
        _fixture.Documents.Items.Should().BeEmpty();
        document.DomainEvents.OfType<DocumentDeletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_AttachmentSentInAMessage_ReturnsSentInMessage()
    {
        var chat = _fixture.SeedChat(_student.Id, Guid.CreateVersion7());
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Chat, chat.Id).Value, _student.Id);
        _fixture.Messages.Seed(ChatMessage.Send(chat, _student.Id, null, [document.Id], TestData.Now).Value);

        var result = await Delete(document, StudentCaller());

        result.Error.Should().Be(ApplicationErrors.Document.SentInMessage);
        _fixture.Documents.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_AttachmentOfAnotherChatMember_ReturnsForbidden()
    {
        var otherMember = Guid.CreateVersion7();
        var chat = _fixture.SeedChat(_student.Id, otherMember);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Chat, chat.Id).Value, otherMember);

        var result = await Delete(document, StudentCaller());

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
        _fixture.Documents.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_ManagerOfTheLeadsBranch_DeletesADocumentAttachedBySomeoneElse()
    {
        var lead = _fixture.SeedLead(DocumentFixture.BranchId);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Lead, lead.Id).Value, Guid.CreateVersion7());

        var result = await Delete(document, _manager);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DocumentDeleted_RemovesTheFile()
    {
        var lead = _fixture.SeedLead(DocumentFixture.BranchId);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Lead, lead.Id).Value, Guid.CreateVersion7());
        document.Delete(TestData.Now).IsSuccess.Should().BeTrue();
        var deleted = document.DomainEvents.OfType<DocumentDeletedDomainEvent>().Single();
        var handler = new DeleteFileWhenDocumentDeletedHandler(_fixture.Storage);

        await handler.Handle(deleted, TestContext.Current.CancellationToken);
        await handler.Handle(deleted, TestContext.Current.CancellationToken);

        _fixture.Storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateDescription_StudentOnTheirContractsDocument_ReturnsForbidden()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Contract, contract.Id).Value, _manager.UserId!.Value);

        var result = await new UpdateDocumentDescriptionCommandHandler(_fixture.CreateAccess(StudentCaller()))
            .Handle(new UpdateDocumentDescriptionCommand(document.Id, "Mine"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.Auth.Forbidden);
    }

    [Fact]
    public async Task GetFile_StudentOfTheContract_OpensTheFile()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Contract, contract.Id).Value, _manager.UserId!.Value);

        var result = await new GetDocumentFileQueryHandler(_fixture.CreateAccess(StudentCaller()), _fixture.Storage)
            .Handle(new GetDocumentFileQuery(document.Id), TestContext.Current.CancellationToken);

        var file = result.Value;
        file.FileName.Should().Be("scan.pdf");
        file.ContentType.Should().Be("application/pdf");
        await using var content = file.Content;
        content.Length.Should().Be(3);
    }

    [Fact]
    public async Task GetFile_OutsiderOfTheContract_ReturnsNotFound()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.Contract, contract.Id).Value, _manager.UserId!.Value);
        var outsider = DocumentFixture.Caller(Guid.CreateVersion7(), UserRole.Student);

        var result = await new GetDocumentFileQueryHandler(_fixture.CreateAccess(outsider), _fixture.Storage)
            .Handle(new GetDocumentFileQuery(document.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Document>());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task GetFile_DocumentOfAnotherUser_IsReadableOnlyIfItIsTheirProfilePhoto(bool isPhoto, bool readable)
    {
        _fixture.Users.Seed(_student);
        var document = _fixture.SeedDocument(EntityReference.Create(EntityKind.User, _student.Id).Value, _student.Id);
        if (isPhoto)
            _student.SetPhoto(document.Id).IsSuccess.Should().BeTrue();

        var otherStudent = DocumentFixture.Caller(Guid.CreateVersion7(), UserRole.Student);
        var result = await new GetDocumentFileQueryHandler(_fixture.CreateAccess(otherStudent), _fixture.Storage)
            .Handle(new GetDocumentFileQuery(document.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().Be(readable);
        if (result.IsSuccess)
            await result.Value.Content.DisposeAsync();
    }

    [Fact]
    public async Task GetDocuments_CallerWhoMayRead_ReadsTheOwnersList()
    {
        var contract = _fixture.SeedContract(_student, DocumentFixture.BranchId);
        var queries = new FakeDocumentQueries();

        var result = await new GetDocumentsQueryHandler(queries, _fixture.CreateOwners(StudentCaller()))
            .Handle(new GetDocumentsQuery(EntityKind.Contract, contract.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        queries.LastOwner.Should().Be(EntityReference.Create(EntityKind.Contract, contract.Id).Value);
    }
}

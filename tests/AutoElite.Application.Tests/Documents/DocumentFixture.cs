using AutoElite.Application.Documents;
using AutoElite.Application.StaffTasks;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Documents;

/// <summary>The records documents are attached to and the stores the document handlers use.</summary>
internal sealed class DocumentFixture
{
    public static readonly Guid BranchId = Guid.CreateVersion7();

    public DocumentFixture()
    {
        Chats = new FakeChatRepository { Messages = Messages };
    }

    public InMemoryRepository<Lead> Leads { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public FakeUserRepository Users { get; } = new();

    public InMemoryRepository<Vehicle> Vehicles { get; } = new();

    public FakeStudyGroupRepository Groups { get; } = new();

    public FakeTheoryLessonRepository Lessons { get; } = new();

    public FakeCourseRepository Courses { get; } = new();

    public InMemoryRepository<ChatMessage> Messages { get; } = new();

    public FakeChatRepository Chats { get; }

    public InMemoryRepository<StaffTask> Tasks { get; } = new();

    public FakeQuestionRepository Questions { get; } = new();

    public FakeDocumentRepository Documents { get; } = new();

    public FakeFileStorage Storage { get; } = new();

    public DocumentOwners CreateOwners(FakeCurrentUser caller) =>
        new(Leads, Contracts, Users, Vehicles, Groups, Lessons, Courses, Chats, Questions, new StaffTaskAccess(Tasks, Users, caller),
            caller);

    public DocumentAccess CreateAccess(FakeCurrentUser caller) => new(Documents, CreateOwners(caller), Users, caller);

    public static FakeCurrentUser Caller(Guid userId, UserRole role, Guid? branchId = null) =>
        new() { UserId = userId, BranchId = branchId, Roles = new HashSet<UserRole> { role } };

    public Contract SeedContract(User student, Guid? branchId)
    {
        Users.Seed(student);
        var course = TestData.NewCourse();
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", student, course, course.Price, Transmission.Manual, branchId,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        Contracts.Seed(contract);

        return contract;
    }

    public Lead SeedLead(Guid? branchId)
    {
        var phone = PhoneNumber.Create("+79991234567").Value;
        var lead = Lead.Create("Anna", phone, null, LeadSource.Website, null, null, branchId, null, TestData.Now).Value;
        Leads.Seed(lead);

        return lead;
    }

    public Chat SeedChat(Guid firstUserId, Guid secondUserId)
    {
        var chat = Chat.CreateDirect(firstUserId, secondUserId, TestData.Now).Value;
        Chats.Seed(chat);

        return chat;
    }

    public Document SeedDocument(EntityReference owner, Guid uploadedByUserId)
    {
        var document = Document.Attach(
                owner, "scan.pdf", "application/pdf", 3, $"documents/{Guid.CreateVersion7():N}", null, uploadedByUserId, TestData.Now)
            .Value;
        Documents.Seed(document);
        Storage.Seed(document.StorageKey, [1, 2, 3]);

        return document;
    }
}

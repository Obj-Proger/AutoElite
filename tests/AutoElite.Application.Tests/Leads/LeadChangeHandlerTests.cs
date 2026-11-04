using AutoElite.Application.Leads.AssignLeadManager;
using AutoElite.Application.Leads.ChangeLeadBranch;
using AutoElite.Application.Leads.UpdateLeadContact;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Leads;

public class LeadChangeHandlerTests
{
    private readonly LeadFixture _fixture = new();

    private Task<Result> UpdateContact(Guid leadId, FakeCurrentUser caller, string? email = "anna@example.com") =>
        new UpdateLeadContactCommandHandler(_fixture.CreateLeadAccess(caller)).Handle(
            new UpdateLeadContactCommand(leadId, "Anna Petrova", null, email, LicenseCategory.A, "Call after 18:00"),
            TestContext.Current.CancellationToken);

    private Task<Result> ChangeBranch(Guid leadId, Guid? branchId, FakeCurrentUser caller) =>
        new ChangeLeadBranchCommandHandler(_fixture.CreateLeadAccess(caller), _fixture.Branches, caller)
            .Handle(new ChangeLeadBranchCommand(leadId, branchId), TestContext.Current.CancellationToken);

    private Task<Result> AssignManager(Guid leadId, Guid managerId, FakeCurrentUser caller) =>
        new AssignLeadManagerCommandHandler(_fixture.CreateLeadAccess(caller), _fixture.CreateLeadManagers(), caller, TestData.Clock())
            .Handle(new AssignLeadManagerCommand(leadId, managerId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task UpdateContact_OpenLead_ReplacesTheContact()
    {
        var lead = _fixture.SeedLead();

        var result = await UpdateContact(lead.Id, TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        lead.ContactName.Should().Be("Anna Petrova");
        lead.Phone.Should().BeNull();
        lead.Email!.Value.Should().Be("anna@example.com");
        lead.DesiredCategory.Should().Be(LicenseCategory.A);
    }

    [Fact]
    public async Task UpdateContact_RemovingTheLastContact_ReturnsContactRequired()
    {
        var lead = _fixture.SeedLead();

        var result = await UpdateContact(lead.Id, TestData.Administrator(), email: null);

        result.Error.Should().Be(DomainErrors.Lead.ContactRequired);
    }

    [Fact]
    public async Task UpdateContact_LeadOfAnotherBranch_ReturnsLeadNotFoundToTheManager()
    {
        var lead = _fixture.SeedLead(Guid.CreateVersion7());

        var result = await UpdateContact(lead.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Lead>());
    }

    [Fact]
    public async Task UpdateContact_LostLead_ReturnsNotOpen()
    {
        var lead = _fixture.SeedLead();
        lead.MarkLost("Chose another school", null, TestData.Now);

        var result = await UpdateContact(lead.Id, TestData.Administrator());

        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
    }

    [Fact]
    public async Task ChangeBranch_ManagerReturnsTheLeadToThePool_Succeeds()
    {
        var branch = _fixture.SeedBranch();
        var lead = _fixture.SeedLead(branch.Id);

        var result = await ChangeBranch(lead.Id, null, TestData.Manager(branch.Id));

        result.IsSuccess.Should().BeTrue();
        lead.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task ChangeBranch_ManagerMovesTheLeadToAnotherBranch_ReturnsBranchNotFound()
    {
        var central = _fixture.SeedBranch("Central");
        var north = _fixture.SeedBranch("North");
        var lead = _fixture.SeedLead(central.Id);

        var result = await ChangeBranch(lead.Id, north.Id, TestData.Manager(central.Id));

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
        lead.BranchId.Should().Be(central.Id);
    }

    [Fact]
    public async Task AssignManager_ManagerOfTheLeadsBranch_AssignsAndRaisesTheEvent()
    {
        var branch = _fixture.SeedBranch();
        var manager = _fixture.SeedManager(branch.Id);
        var lead = _fixture.SeedLead(branch.Id);
        var administrator = TestData.Administrator();

        var result = await AssignManager(lead.Id, manager.UserId!.Value, administrator);

        result.IsSuccess.Should().BeTrue();
        lead.ManagerId.Should().Be(manager.UserId);
        lead.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<LeadAssignedDomainEvent>()
            .Which.AssignedByUserId.Should().Be(administrator.UserId);
    }

    [Fact]
    public async Task AssignManager_SameManagerAgain_ReturnsManagerAlreadyAssigned()
    {
        var manager = _fixture.SeedManager(null);
        var lead = _fixture.SeedLead();
        await AssignManager(lead.Id, manager.UserId!.Value, TestData.Administrator());

        var result = await AssignManager(lead.Id, manager.UserId.Value, TestData.Administrator());

        result.Error.Should().Be(DomainErrors.Lead.ManagerAlreadyAssigned);
    }

    [Fact]
    public async Task AssignManager_DeactivatedManager_ReturnsNotAManager()
    {
        var manager = _fixture.SeedManager(null);
        _fixture.Users.Items.Single(user => user.Id == manager.UserId).Deactivate(TestData.Now);
        var lead = _fixture.SeedLead();

        var result = await AssignManager(lead.Id, manager.UserId!.Value, TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Lead.NotAManager);
        lead.ManagerId.Should().BeNull();
    }
}

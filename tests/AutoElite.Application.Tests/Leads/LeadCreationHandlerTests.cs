using AutoElite.Application.Leads.CreateLead;
using AutoElite.Application.Leads.SubmitLeadRequest;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Leads;

public class LeadCreationHandlerTests
{
    private readonly LeadFixture _fixture = new();

    private Task<Result> Submit(SubmitLeadRequestCommand command) =>
        new SubmitLeadRequestCommandHandler(_fixture.Leads, _fixture.Branches, TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);

    private Task<Result<Guid>> Create(CreateLeadCommand command, FakeCurrentUser caller) =>
        new CreateLeadCommandHandler(_fixture.Leads, _fixture.Branches, _fixture.CreateLeadManagers(), caller, TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);

    private static CreateLeadCommand CreateCommand(Guid? branchId = null, Guid? managerId = null) =>
        new("Anna", "+79991234567", null, LeadSource.PhoneCall, LicenseCategory.B, null, branchId, managerId);

    [Fact]
    public async Task Submit_PublicRequest_CreatesNewWebsiteLeadWithoutAuthor()
    {
        var branch = _fixture.SeedBranch();

        var result = await Submit(new SubmitLeadRequestCommand("Anna", null, "anna@example.com", LicenseCategory.B, "Evenings", branch.Id));

        result.IsSuccess.Should().BeTrue();
        var lead = _fixture.Leads.Items.Should().ContainSingle().Which;
        lead.Source.Should().Be(LeadSource.Website);
        lead.Status.Should().Be(LeadStatus.New);
        lead.Email!.Value.Should().Be("anna@example.com");
        lead.BranchId.Should().Be(branch.Id);
        lead.ManagerId.Should().BeNull();
        lead.StatusHistory.Should().ContainSingle().Which.ChangedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task Submit_NoContact_ReturnsContactRequired()
    {
        var result = await Submit(new SubmitLeadRequestCommand("Anna", " ", null, null, null, null));

        result.Error.Should().Be(DomainErrors.Lead.ContactRequired);
        _fixture.Leads.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_InactiveBranch_ReturnsBranchInactive()
    {
        var branch = _fixture.SeedBranch();
        branch.Deactivate();

        var result = await Submit(new SubmitLeadRequestCommand("Anna", "+79991234567", null, null, null, branch.Id));

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
    }

    [Fact]
    public async Task Create_ByManager_UsesTheManagersBranchAndAssignsTheManager()
    {
        var branch = _fixture.SeedBranch();
        var manager = _fixture.SeedManager(branch.Id);

        var result = await Create(CreateCommand(), manager);

        var lead = _fixture.Leads.Items.Should().ContainSingle().Which;
        lead.Id.Should().Be(result.Value);
        lead.BranchId.Should().Be(branch.Id);
        lead.ManagerId.Should().Be(manager.UserId);
        lead.StatusHistory.Single().ChangedByUserId.Should().Be(manager.UserId);
        lead.DomainEvents.OfType<LeadAssignedDomainEvent>().Should().ContainSingle()
            .Which.AssignedByUserId.Should().Be(manager.UserId);
    }

    [Fact]
    public async Task Create_ByAdministratorWithoutManager_LeavesTheLeadUnassigned()
    {
        var result = await Create(CreateCommand(), TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        _fixture.Leads.Items.Single().ManagerId.Should().BeNull();
    }

    [Fact]
    public async Task Create_ManagerOfAnotherBranch_ReturnsManagerOfAnotherBranch()
    {
        var central = _fixture.SeedBranch("Central");
        var north = _fixture.SeedBranch("North");
        var manager = _fixture.SeedManager(north.Id);

        var result = await Create(CreateCommand(central.Id, manager.UserId), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Lead.ManagerOfAnotherBranch);
        _fixture.Leads.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ResponsibleUserIsNotAManager_ReturnsNotAManager()
    {
        var student = TestData.NewUser("student@example.com");
        _fixture.Users.Seed(student);
        _fixture.Roles.Seed(student.Id, UserRole.Student);

        var result = await Create(CreateCommand(null, student.Id), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Lead.NotAManager);
    }

    [Fact]
    public async Task Create_ManagerIntoAnotherBranch_ReturnsBranchNotFound()
    {
        var central = _fixture.SeedBranch("Central");
        var north = _fixture.SeedBranch("North");
        var manager = _fixture.SeedManager(north.Id);

        var result = await Create(CreateCommand(central.Id), manager);

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public void CreateValidator_InvalidValues_ReportsEach()
    {
        var command = new CreateLeadCommand(
            "Anna", "8 999", "not-an-email", (LeadSource)99, (LicenseCategory)99, null, Guid.Empty, Guid.Empty);

        var codes = new CreateLeadCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().BeEquivalentTo(new[]
        {
            DomainErrors.PhoneNumber.InvalidFormat.Code,
            DomainErrors.Email.InvalidFormat.Code,
            DomainErrors.Lead.SourceNotDefined.Code,
            DomainErrors.Lead.CategoryNotDefined.Code,
            DomainErrors.Lead.BranchIdEmpty.Code,
            DomainErrors.Lead.ManagerIdEmpty.Code
        });
    }
}

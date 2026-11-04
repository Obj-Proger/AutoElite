using AutoElite.Application.Contracts.ConvertLeadToContract;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Contracts;

public class ConvertLeadToContractCommandHandlerTests
{
    private readonly ContractFixture _fixture = new();
    private readonly FakeCurrentUser _manager = TestData.Manager(null);

    private Task<Result<LeadConversion>> Handle(ConvertLeadToContractCommand command, FakeCurrentUser? caller = null)
    {
        var user = caller ?? _manager;

        return new ConvertLeadToContractCommandHandler(
                _fixture.CreateLeadAccess(user),
                _fixture.CreateContractStudents(user),
                _fixture.CreateUserRegistration(),
                _fixture.CreateContractFactory(),
                _fixture.Branches,
                user,
                TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);
    }

    private ConvertLeadToContractCommand Command(Guid leadId, Guid? existingStudentId = null) => new(
        leadId, existingStudentId, "Petrova", "Anna", null, null, null, Language.Russian,
        _fixture.Course.Id, Transmission.Manual, ContractFixture.Start, ContractFixture.End);

    private Lead SeedLead(Guid? branchId = null)
    {
        var lead = Lead.Create(
                "Anna", PhoneNumber.Create("+79991234567").Value, Email.Create("anna@example.com").Value,
                LeadSource.Website, LicenseCategory.B, null, branchId, null, TestData.Now)
            .Value;
        _fixture.Leads.Seed(lead);

        return lead;
    }

    [Fact]
    public async Task Handle_NewStudent_RegistersTheStudentFromTheLeadAndConvertsIt()
    {
        var branch = _fixture.SeedBranch();
        var lead = SeedLead(branch.Id);

        var result = await Handle(Command(lead.Id));

        result.IsSuccess.Should().BeTrue();
        var student = _fixture.Users.Items.Should().ContainSingle().Which;
        student.Id.Should().Be(result.Value.StudentId);
        student.FullName.Should().Be(FullName.Create("Petrova", "Anna").Value);
        student.Email.Value.Should().Be("anna@example.com");
        student.Phone!.Value.Should().Be("+79991234567");
        student.BranchId.Should().Be(branch.Id);
        (await _fixture.Roles.GetRolesAsync(student.Id, TestContext.Current.CancellationToken)).Should().Equal(UserRole.Student);
        _fixture.Identity.Accounts.Should().ContainKey(student.Id);

        var contract = _fixture.Contracts.Items.Should().ContainSingle().Which;
        contract.Id.Should().Be(result.Value.ContractId);
        contract.StudentId.Should().Be(student.Id);
        contract.BranchId.Should().Be(branch.Id);

        lead.Status.Should().Be(LeadStatus.Converted);
        lead.ContractId.Should().Be(contract.Id);
        lead.StatusHistory[^1].ChangedByUserId.Should().Be(_manager.UserId);
    }

    [Fact]
    public async Task Handle_GivenEmail_UsesItInsteadOfTheLeads()
    {
        var lead = SeedLead();

        await Handle(Command(lead.Id) with { Email = "anna.petrova@example.com" });

        _fixture.Users.Items.Single().Email.Value.Should().Be("anna.petrova@example.com");
    }

    [Fact]
    public async Task Handle_ExistingStudent_DrawsUpTheContractForThem()
    {
        var student = _fixture.SeedStudent("anna@example.com");
        var lead = SeedLead();

        var result = await Handle(Command(lead.Id, student.Id));

        result.Value.StudentId.Should().Be(student.Id);
        _fixture.Users.Items.Should().ContainSingle();
        _fixture.Contracts.Items.Single().StudentId.Should().Be(student.Id);
    }

    [Fact]
    public async Task Handle_EmailOfAnotherUser_ReturnsEmailTakenAndLeavesTheLeadOpen()
    {
        _fixture.SeedStudent("anna@example.com");
        var lead = SeedLead();

        var result = await Handle(Command(lead.Id));

        result.Error.Should().Be(ApplicationErrors.Account.EmailTaken);
        lead.IsOpen.Should().BeTrue();
        _fixture.Contracts.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LostLead_ReturnsNotOpenAndRegistersNobody()
    {
        var lead = SeedLead();
        lead.MarkLost("Moved away", null, TestData.Now);

        var result = await Handle(Command(lead.Id));

        result.Error.Should().Be(DomainErrors.Lead.NotOpen);
        _fixture.Users.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LeadOfAnotherBranch_ReturnsLeadNotFound()
    {
        var lead = SeedLead(Guid.CreateVersion7());

        var result = await Handle(Command(lead.Id), TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Lead>());
    }

    [Fact]
    public void Validator_NewStudentWithoutNames_ReportsTheNames()
    {
        var command = Command(Guid.CreateVersion7()) with { LastName = "", FirstName = null };

        var codes = new ConvertLeadToContractCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.FullName.LastNameEmpty.Code, DomainErrors.FullName.FirstNameEmpty.Code);
    }

    [Fact]
    public void Validator_ExistingStudent_SkipsTheNewStudentDetails()
    {
        var command = Command(Guid.CreateVersion7(), Guid.CreateVersion7()) with { LastName = "", FirstName = null };

        new ConvertLeadToContractCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }
}

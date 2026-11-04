using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Leads;
using AutoElite.Application.Leads.GetLead;
using AutoElite.Application.Leads.GetLeads;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Leads;

public class LeadQueryHandlerTests
{
    private readonly FakeLeadQueries _queries = new();

    private Task<Result<PagedList<LeadListItem>>> GetLeads(GetLeadsQuery query, FakeCurrentUser caller) =>
        new GetLeadsQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    private Task<Result<LeadDetails>> GetLead(Guid leadId, FakeCurrentUser caller) =>
        new GetLeadQueryHandler(_queries, caller).Handle(new GetLeadQuery(leadId), TestContext.Current.CancellationToken);

    private LeadDetails SeedDetails(Guid? branchId)
    {
        var details = new LeadDetails(
            Guid.CreateVersion7(), "Anna", "+79991234567", null, LeadSource.PhoneCall, null, null, LeadStatus.New,
            null, null, branchId, null, null, null, TestData.Now, [], []);
        _queries.Seed(details);

        return details;
    }

    [Fact]
    public async Task GetLeads_Filters_PassesThemWithTrimmedSearchAndTheScope()
    {
        var branchId = Guid.CreateVersion7();
        var managerId = Guid.CreateVersion7();

        await GetLeads(
            new GetLeadsQuery(" anna ", LeadStatus.InProgress, LeadSource.Website, managerId, branchId, 3, 10),
            TestData.Manager(branchId));

        _queries.LastListCall.Should().Be((
            new LeadListFilter("anna", LeadStatus.InProgress, LeadSource.Website, managerId, branchId),
            DataScope.ForBranch(branchId),
            new PageRequest(3, 10)));
    }

    [Fact]
    public async Task GetLeads_ManagerFiltersByAnotherBranch_ReturnsAnEmptyPageWithoutReading()
    {
        var result = await GetLeads(new GetLeadsQuery(BranchId: Guid.CreateVersion7()), TestData.Manager(Guid.CreateVersion7()));

        result.Value.Items.Should().BeEmpty();
        _queries.LastListCall.Should().BeNull();
    }

    [Fact]
    public async Task GetLead_LeadInThePool_IsVisibleToEveryManager()
    {
        var details = SeedDetails(null);

        var result = await GetLead(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetLead_LeadOfAnotherBranch_ReturnsLeadNotFoundToTheManager()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await GetLead(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Lead>());
    }

    [Fact]
    public void Validator_UndefinedStatusAndEmptyIdentifiers_ReportsEach()
    {
        var query = new GetLeadsQuery(Status: (LeadStatus)99, Source: (LeadSource)99, ManagerId: Guid.Empty, BranchId: Guid.Empty);

        var codes = new GetLeadsQueryValidator().Validate(query).Errors.Select(failure => failure.ErrorCode);

        codes.Should().BeEquivalentTo(new[]
        {
            ApplicationErrors.Lead.StatusNotDefined.Code,
            DomainErrors.Lead.SourceNotDefined.Code,
            DomainErrors.Lead.ManagerIdEmpty.Code,
            DomainErrors.Lead.BranchIdEmpty.Code
        });
    }
}

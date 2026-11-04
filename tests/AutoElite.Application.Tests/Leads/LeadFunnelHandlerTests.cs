using AutoElite.Application.Leads.AddLeadNote;
using AutoElite.Application.Leads.MarkLeadLost;
using AutoElite.Application.Leads.ReopenLead;
using AutoElite.Application.Leads.StartLeadWork;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Leads;

public class LeadFunnelHandlerTests
{
    private readonly LeadFixture _fixture = new();
    private readonly FakeCurrentUser _manager = TestData.Manager(null);

    private Task<Result> StartWork(Guid leadId, FakeCurrentUser? caller = null)
    {
        var user = caller ?? _manager;

        return new StartLeadWorkCommandHandler(_fixture.CreateLeadAccess(user), user, TestData.Clock())
            .Handle(new StartLeadWorkCommand(leadId), TestContext.Current.CancellationToken);
    }

    private Task<Result> MarkLost(Guid leadId, string? reason) =>
        new MarkLeadLostCommandHandler(_fixture.CreateLeadAccess(_manager), _manager, TestData.Clock())
            .Handle(new MarkLeadLostCommand(leadId, reason), TestContext.Current.CancellationToken);

    private Task<Result> Reopen(Guid leadId) =>
        new ReopenLeadCommandHandler(_fixture.CreateLeadAccess(_manager), _manager, TestData.Clock())
            .Handle(new ReopenLeadCommand(leadId), TestContext.Current.CancellationToken);

    private Task<Result<Guid>> AddNote(Guid leadId, string? text) =>
        new AddLeadNoteCommandHandler(_fixture.CreateLeadAccess(_manager), _manager, TestData.Clock())
            .Handle(new AddLeadNoteCommand(leadId, text), TestContext.Current.CancellationToken);

    [Fact]
    public async Task StartWork_NewLead_MovesItInProgressByTheCaller()
    {
        var lead = _fixture.SeedLead();

        var result = await StartWork(lead.Id);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.InProgress);
        var change = lead.StatusHistory[^1];
        change.FromStatus.Should().Be(LeadStatus.New);
        change.ChangedByUserId.Should().Be(_manager.UserId);
        change.ChangedAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task StartWork_LeadAlreadyInProgress_ReturnsNotNew()
    {
        var lead = _fixture.SeedLead();
        await StartWork(lead.Id);

        var result = await StartWork(lead.Id);

        result.Error.Should().Be(DomainErrors.Lead.NotNew);
    }

    [Fact]
    public async Task StartWork_LeadOfAnotherBranch_ReturnsLeadNotFound()
    {
        var lead = _fixture.SeedLead(Guid.CreateVersion7());

        var result = await StartWork(lead.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Lead>());
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public async Task MarkLost_WithReason_RecordsTheLoss()
    {
        var lead = _fixture.SeedLead();

        var result = await MarkLost(lead.Id, "Too expensive");

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Lost);
        lead.LostReason.Should().Be("Too expensive");
        lead.StatusHistory[^1].Reason.Should().Be("Too expensive");
    }

    [Fact]
    public async Task MarkLost_WithoutReason_ReturnsLostReasonEmpty()
    {
        var lead = _fixture.SeedLead();

        var result = await MarkLost(lead.Id, " ");

        result.Error.Should().Be(DomainErrors.Lead.LostReasonEmpty);
    }

    [Fact]
    public async Task Reopen_LostLead_ReturnsItToWork()
    {
        var lead = _fixture.SeedLead();
        await MarkLost(lead.Id, "No answer");

        var result = await Reopen(lead.Id);

        result.IsSuccess.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.InProgress);
        lead.LostReason.Should().BeNull();
    }

    [Fact]
    public async Task Reopen_OpenLead_ReturnsNotLost()
    {
        var lead = _fixture.SeedLead();

        var result = await Reopen(lead.Id);

        result.Error.Should().Be(DomainErrors.Lead.NotLost);
    }

    [Fact]
    public async Task AddNote_AnyStage_AddsItWithTheCallerAsAuthor()
    {
        var lead = _fixture.SeedLead();
        await MarkLost(lead.Id, "No answer");

        var result = await AddNote(lead.Id, "Called twice");

        var note = lead.Notes.Should().ContainSingle().Which;
        note.Id.Should().Be(result.Value);
        note.Text.Should().Be("Called twice");
        note.AuthorId.Should().Be(_manager.UserId!.Value);
        note.WrittenAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task AddNote_EmptyText_ReturnsTextEmpty()
    {
        var lead = _fixture.SeedLead();

        var result = await AddNote(lead.Id, "");

        result.Error.Should().Be(DomainErrors.LeadNote.TextEmpty);
    }
}

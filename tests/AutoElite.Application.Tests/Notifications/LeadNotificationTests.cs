using AutoElite.Application.Notifications.Leads;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class LeadNotificationTests
{
    private readonly NotificationFixture _fixture = new();
    private readonly InMemoryRepository<Lead> _leads = new();

    private Lead SeedLead(Guid? branchId, LeadSource source = LeadSource.Website)
    {
        var lead = Lead.Create("Anna", PhoneNumber.Create("+79991234567").Value, null, source, null, null, branchId, null, TestData.Now)
            .Value;
        _leads.Seed(lead);

        return lead;
    }

    private Task Created(Lead lead) =>
        new NotifyManagersWhenLeadCreatedHandler(_leads, _fixture.Roles, _fixture.CreateNotifier()).Handle(
            lead.DomainEvents.OfType<LeadCreatedDomainEvent>().Single(), TestContext.Current.CancellationToken);

    private Task Assigned(Lead lead) =>
        new NotifyWhenLeadAssignedHandler(_leads, _fixture.CreateNotifier()).Handle(
            lead.DomainEvents.OfType<LeadAssignedDomainEvent>().Last(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task LeadCreated_Unassigned_NotifiesTheManagersWhoCanSeeIt()
    {
        var ownManager = _fixture.SeedManager("own@example.com", _fixture.Branch.Id);
        var schoolManager = _fixture.SeedManager("school@example.com", null, Language.English);
        var otherManager = _fixture.SeedManager("other@example.com", Guid.CreateVersion7());
        var lead = SeedLead(_fixture.Branch.Id);

        await Created(lead);

        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([ownManager.Id, schoolManager.Id]);
        _fixture.Of(otherManager.Id).Should().BeEmpty();
        var russian = _fixture.Of(ownManager.Id).Single();
        russian.Type.Should().Be(NotificationType.NewLead);
        russian.Title.Should().Be("Новая заявка");
        russian.Body.Should().Be("Заявка от Anna. Источник: сайт.");
        russian.Subject.Should().Be(EntityReference.Create(EntityKind.Lead, lead.Id).Value);
        _fixture.Of(schoolManager.Id).Single().Body.Should().Be("A request from Anna. Source: website.");
    }

    [Fact]
    public async Task LeadCreated_WithoutBranch_NotifiesEveryActiveManager()
    {
        var first = _fixture.SeedManager("first@example.com", _fixture.Branch.Id);
        var second = _fixture.SeedManager("second@example.com", Guid.CreateVersion7());
        _fixture.SeedManager("gone@example.com", null).Deactivate(TestData.Now);

        await Created(SeedLead(null));

        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([first.Id, second.Id]);
    }

    [Fact]
    public async Task LeadCreated_AlreadyAssigned_NotifiesNobody()
    {
        var manager = _fixture.SeedManager("own@example.com", _fixture.Branch.Id);
        var lead = SeedLead(_fixture.Branch.Id);
        lead.AssignManager(manager.Id, manager.Id, TestData.Now);

        await Created(lead);

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LeadAssigned_BySomeoneElse_NotifiesTheManager()
    {
        var manager = _fixture.SeedManager("own@example.com", _fixture.Branch.Id);
        var lead = SeedLead(_fixture.Branch.Id);
        lead.AssignManager(manager.Id, Guid.CreateVersion7(), TestData.Now);

        await Assigned(lead);

        var notification = _fixture.Of(manager.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.LeadAssigned);
        notification.Body.Should().Be("Теперь вы отвечаете за заявку от Anna.");
    }

    [Fact]
    public async Task LeadAssigned_TakenByTheManagerThemselves_NotifiesNobody()
    {
        var manager = _fixture.SeedManager("own@example.com", _fixture.Branch.Id);
        var lead = SeedLead(_fixture.Branch.Id);
        lead.AssignManager(manager.Id, manager.Id, TestData.Now);

        await Assigned(lead);

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LeadAssigned_ReassignedSinceThen_DoesNotNotifyThePreviousManager()
    {
        var first = _fixture.SeedManager("first@example.com", _fixture.Branch.Id);
        var second = _fixture.SeedManager("second@example.com", _fixture.Branch.Id);
        var lead = SeedLead(_fixture.Branch.Id);
        lead.AssignManager(first.Id, null, TestData.Now);
        var firstAssignment = lead.DomainEvents.OfType<LeadAssignedDomainEvent>().Single();
        lead.AssignManager(second.Id, null, TestData.Now);

        await new NotifyWhenLeadAssignedHandler(_leads, _fixture.CreateNotifier())
            .Handle(firstAssignment, TestContext.Current.CancellationToken);

        _fixture.Notifications.Items.Should().BeEmpty();
    }
}

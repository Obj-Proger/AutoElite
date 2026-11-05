using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Leads;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class LeadPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly PageRequest _firstPage = new(1, PageRequest.MaxPageSize);

    // Every test names its leads with a marker of its own, so the leads of other tests stay out of its lists.
    private readonly string _marker = Guid.NewGuid().ToString("N")[..12];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateTime Now => FixedClock.Default.UtcNow;

    private Lead NewLead(
        string name = "Анна", Guid? branchId = null, LeadSource source = LeadSource.Website, string? phone = "+79991112233",
        string? email = null) =>
        Lead.Create(
                $"{name} {_marker}", phone is null ? null : PhoneNumber.Create(phone).Value,
                email is null ? null : Email.Create(email).Value, source, LicenseCategory.B, "Хочет учиться вечером",
                branchId, null, Now)
            .Value;

    private static User NewUser(string lastName) =>
        User.Create(FullName.Create(lastName, "Мария", "Ивановна").Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value,
                null, null, Now)
            .Value;

    private async Task SaveAsync(TimeProvider? clock, params object[] records)
    {
        await using var context = database.CreateContext(timeProvider: clock);
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    /// <summary>Loads the lead, changes it and saves, as one request.</summary>
    private async Task ChangeAsync(Guid leadId, Action<Lead> change, TimeProvider? clock = null)
    {
        await using var context = database.CreateContext(timeProvider: clock);
        var lead = await new LeadRepository(context).GetByIdAsync(leadId, CancellationToken);
        change(lead!);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private async Task<Lead> LoadAsync(Guid leadId)
    {
        await using var context = database.CreateContext();

        return (await new LeadRepository(context).GetByIdAsync(leadId, CancellationToken))!;
    }

    [Fact]
    public async Task Save_NewLead_StoresItsValuesHistoryAndCreationEvent()
    {
        var lead = NewLead(email: "anna@example.com");
        await SaveAsync(null, lead);

        var loaded = await LoadAsync(lead.Id);

        loaded.ContactName.Should().Be(lead.ContactName);
        loaded.Phone!.Value.Should().Be("+79991112233");
        loaded.Email!.Value.Should().Be("anna@example.com");
        loaded.DesiredCategory.Should().Be(LicenseCategory.B);
        loaded.Comment.Should().Be("Хочет учиться вечером");
        loaded.Status.Should().Be(LeadStatus.New);
        loaded.Notes.Should().BeEmpty();
        loaded.StatusHistory.Should().ContainSingle()
            .Which.Should().Match<LeadStatusChange>(change => change.FromStatus == null && change.ToStatus == LeadStatus.New);

        await using var context = database.CreateContext();
        var messages = await context.Set<OutboxMessage>()
            .Where(message => message.Type == nameof(LeadCreatedDomainEvent))
            .ToListAsync(CancellationToken);
        messages.Should().Contain(message =>
            ((LeadCreatedDomainEvent)OutboxSerializer.Deserialize(message.Type, message.Payload)).LeadId == lead.Id);
    }

    [Fact]
    public async Task Save_NotesAndStatusChanges_AreStoredWithTheLead()
    {
        var author = NewUser("Петрова");
        var lead = NewLead();
        await SaveAsync(null, author, lead);

        await ChangeAsync(lead.Id, tracked =>
        {
            tracked.StartWork(author.Id, Now).IsSuccess.Should().BeTrue();
            tracked.AddNote("Перезвонить в понедельник", author.Id, Now).IsSuccess.Should().BeTrue();
        });
        await ChangeAsync(lead.Id, tracked => tracked.MarkLost("Выбрал другую школу", author.Id, Now).IsSuccess.Should().BeTrue());

        var loaded = await LoadAsync(lead.Id);
        loaded.Notes.Should().ContainSingle().Which.Text.Should().Be("Перезвонить в понедельник");
        loaded.StatusHistory.Select(change => change.ToStatus)
            .Should().BeEquivalentTo(new[] { LeadStatus.New, LeadStatus.InProgress, LeadStatus.Lost });
        loaded.LostReason.Should().Be("Выбрал другую школу");
    }

    [Fact]
    public async Task Save_OnlyANewNote_MarksTheLeadChanged()
    {
        var author = NewUser("Сидорова");
        var lead = NewLead();
        await SaveAsync(null, author, lead);
        var later = new FixedClock(FixedClock.Default.GetUtcNow().AddHours(3));

        await ChangeAsync(lead.Id, tracked => tracked.AddNote("Запросила цены", author.Id, Now).IsSuccess.Should().BeTrue(), later);

        (await LoadAsync(lead.Id)).ModifiedAtUtc.Should().Be(later.UtcNow);
    }

    [Fact]
    public async Task Save_NotesAddedToAStaleCopy_ThrowsConcurrencyConflict()
    {
        var author = NewUser("Кузнецова");
        var lead = NewLead();
        await SaveAsync(null, author, lead);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstCopy = await new LeadRepository(first).GetByIdAsync(lead.Id, CancellationToken);
        var secondCopy = await new LeadRepository(second).GetByIdAsync(lead.Id, CancellationToken);
        firstCopy!.AddNote("Первая", author.Id, Now).IsSuccess.Should().BeTrue();
        secondCopy!.AddNote("Вторая", author.Id, Now).IsSuccess.Should().BeTrue();
        await new UnitOfWork(first).SaveChangesAsync(CancellationToken);

        var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task GetDetails_ReadsTheNamesOfBranchManagerAndAuthors()
    {
        var branch = Branch.Create($"Branch {_marker}", Address.Create("Екатеринбург", "ул. Ленина, 1").Value, null, "Asia/Yekaterinburg")
            .Value;
        var manager = NewUser("Морозова");
        var lead = NewLead(branchId: branch.Id);
        await SaveAsync(null, branch, manager, lead);
        await ChangeAsync(lead.Id, tracked =>
        {
            tracked.AssignManager(manager.Id, manager.Id, Now).IsSuccess.Should().BeTrue();
            tracked.StartWork(manager.Id, Now).IsSuccess.Should().BeTrue();
            tracked.AddNote("Записать на консультацию", manager.Id, Now).IsSuccess.Should().BeTrue();
        });
        await using var context = database.CreateContext();

        var details = await new LeadQueries(context).GetDetailsAsync(lead.Id, CancellationToken);

        details!.BranchName.Should().Be(branch.Name);
        details.ManagerId.Should().Be(manager.Id);
        details.ManagerName.Should().Be("Морозова Мария Ивановна");
        details.Phone.Should().Be("+79991112233");
        details.Notes.Should().ContainSingle().Which.AuthorName.Should().Be("Морозова Мария Ивановна");
        details.StatusHistory.Select(change => change.ChangedByName).Should().Equal(null, "Морозова Мария Ивановна");
        (await new LeadQueries(context).GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetList_SearchesNamePhoneAndEmailAndShowsTheNewestFirst()
    {
        var older = NewLead("Борис", phone: "+79995550101");
        var newer = NewLead("Вера", phone: null, email: $"vera-{_marker}@example.com");
        await SaveAsync(FixedClock.Default, older);
        await SaveAsync(new FixedClock(FixedClock.Default.GetUtcNow().AddMinutes(5)), newer);
        await using var context = database.CreateContext();
        var queries = new LeadQueries(context);

        var byMarker = await queries.GetListAsync(Filter(_marker.ToUpperInvariant()), DataScope.SchoolWide, _firstPage, CancellationToken);
        var byPhone = await queries.GetListAsync(Filter("5550101"), DataScope.SchoolWide, _firstPage, CancellationToken);

        byMarker.Items.Select(item => item.Id).Should().Equal(newer.Id, older.Id);
        byMarker.Items[0].Email.Should().Be($"vera-{_marker}@example.com");
        byPhone.Items.Select(item => item.Id).Should().Contain(older.Id).And.NotContain(newer.Id);
    }

    [Fact]
    public async Task GetList_FiltersByStatusSourceBranchAndScope()
    {
        var branch = Branch.Create($"Branch {_marker}", Address.Create("Екатеринбург", "ул. Мира, 2").Value, null, "Asia/Yekaterinburg")
            .Value;
        var otherBranch = Branch.Create($"Other {_marker}", Address.Create("Пермь", "ул. Мира, 3").Value, null, "Asia/Yekaterinburg")
            .Value;
        var ofBranch = NewLead("Галина", branch.Id, LeadSource.PhoneCall);
        var withoutBranch = NewLead("Дмитрий");
        var ofOtherBranch = NewLead("Елена", otherBranch.Id);
        await SaveAsync(null, branch, otherBranch, ofBranch, withoutBranch, ofOtherBranch);
        await ChangeAsync(withoutBranch.Id, tracked => tracked.StartWork(null, Now).IsSuccess.Should().BeTrue());
        await using var context = database.CreateContext();
        var queries = new LeadQueries(context);

        async Task<IEnumerable<Guid>> IdsAsync(LeadListFilter filter, DataScope? scope = null) =>
            (await queries.GetListAsync(filter, scope ?? DataScope.SchoolWide, _firstPage, CancellationToken))
                .Items.Select(item => item.Id);

        (await IdsAsync(Filter(_marker) with { Status = LeadStatus.InProgress })).Should().Equal(withoutBranch.Id);
        (await IdsAsync(Filter(_marker) with { Source = LeadSource.PhoneCall })).Should().Equal(ofBranch.Id);
        (await IdsAsync(Filter(_marker) with { BranchId = otherBranch.Id })).Should().Equal(ofOtherBranch.Id);
        (await IdsAsync(Filter(_marker), DataScope.ForBranch(branch.Id))).Should().BeEquivalentTo(new[] { ofBranch.Id, withoutBranch.Id });
    }

    private static LeadListFilter Filter(string search) => new(search, null, null, null, null);
}

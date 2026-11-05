using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Leads;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads leads with the names of their branch, manager and the authors in their journal, without tracking.</summary>
internal sealed class LeadQueries(AutoEliteDbContext dbContext) : ILeadQueries
{
    /// <inheritdoc />
    public async Task<PagedList<LeadListItem>> GetListAsync(
        LeadListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var leads = dbContext.Set<Lead>().AsNoTracking();
        if (!scope.IsSchoolWide)
            leads = leads.Where(lead => lead.BranchId == null || lead.BranchId == scope.BranchId);

        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
        {
            // Phone and email are value objects stored as text; the casts let SQL search them.
            leads = leads.Where(lead =>
                EF.Functions.ILike(lead.ContactName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike((string)(object)lead.Phone!, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike((string)(object)lead.Email!, pattern, QueryExtensions.LikeEscape));
        }

        if (filter.Status is { } status)
            leads = leads.Where(lead => lead.Status == status);

        if (filter.Source is { } source)
            leads = leads.Where(lead => lead.Source == source);

        if (filter.ManagerId is { } managerId)
            leads = leads.Where(lead => lead.ManagerId == managerId);

        if (filter.BranchId is { } branchId)
            leads = leads.Where(lead => lead.BranchId == branchId);

        var ordered = Rows(leads).OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id);
        var (rows, totalCount) = await ordered.ReadPageAsync(page, cancellationToken);
        LeadListItem[] items =
        [
            .. rows.Select(row => new LeadListItem(
                row.Id, row.ContactName, row.Phone?.Value, row.Email?.Value, row.Source, row.DesiredCategory, row.Status,
                row.BranchId, row.BranchName, row.ManagerId,
                PersonNames.Format(row.ManagerLastName, row.ManagerFirstName, row.ManagerMiddleName), row.CreatedAtUtc))
        ];

        return new PagedList<LeadListItem>(items, page, totalCount);
    }

    /// <inheritdoc />
    public async Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var row = await Rows(dbContext.Set<Lead>().AsNoTracking().Where(lead => lead.Id == leadId))
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var notes = await (
                from note in dbContext.Set<LeadNote>().AsNoTracking()
                join author in dbContext.Set<User>() on note.AuthorId equals author.Id
                where EF.Property<Guid>(note, LeadConfiguration.LeadKey) == leadId
                orderby note.WrittenAtUtc, note.Id
                select new
                {
                    note.Id,
                    note.Text,
                    note.AuthorId,
                    author.FullName.LastName,
                    author.FullName.FirstName,
                    author.FullName.MiddleName,
                    note.WrittenAtUtc
                })
            .ToListAsync(cancellationToken);

        var history = await (
                from change in dbContext.Set<LeadStatusChange>().AsNoTracking()
                join user in dbContext.Set<User>() on change.ChangedByUserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                where EF.Property<Guid>(change, LeadConfiguration.LeadKey) == leadId
                orderby change.ChangedAtUtc, change.Id
                select new
                {
                    change.FromStatus,
                    change.ToStatus,
                    change.ChangedAtUtc,
                    change.ChangedByUserId,
                    LastName = user == null ? null : user.FullName.LastName,
                    FirstName = user == null ? null : user.FullName.FirstName,
                    MiddleName = user == null ? null : user.FullName.MiddleName,
                    change.Reason
                })
            .ToListAsync(cancellationToken);

        return new LeadDetails(
            row.Id, row.ContactName, row.Phone?.Value, row.Email?.Value, row.Source, row.DesiredCategory, row.Comment,
            row.Status, row.LostReason, row.ContractId, row.BranchId, row.BranchName, row.ManagerId,
            PersonNames.Format(row.ManagerLastName, row.ManagerFirstName, row.ManagerMiddleName), row.CreatedAtUtc,
            [
                .. notes.Select(note => new LeadNoteItem(
                    note.Id, note.Text, note.AuthorId, PersonNames.Format(note.LastName, note.FirstName, note.MiddleName)!,
                    note.WrittenAtUtc))
            ],
            [
                .. history.Select(change => new LeadStatusChangeItem(
                    change.FromStatus, change.ToStatus, change.ChangedAtUtc, change.ChangedByUserId,
                    PersonNames.Format(change.LastName, change.FirstName, change.MiddleName), change.Reason))
            ]);
    }

    private IQueryable<LeadRow> Rows(IQueryable<Lead> leads) =>
        from lead in leads
        join branch in dbContext.Set<Branch>() on lead.BranchId equals (Guid?)branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        join manager in dbContext.Set<User>() on lead.ManagerId equals (Guid?)manager.Id into managers
        from manager in managers.DefaultIfEmpty()
        select new LeadRow
        {
            Id = lead.Id,
            ContactName = lead.ContactName,
            Phone = lead.Phone,
            Email = lead.Email,
            Source = lead.Source,
            DesiredCategory = lead.DesiredCategory,
            Comment = lead.Comment,
            Status = lead.Status,
            LostReason = lead.LostReason,
            ContractId = lead.ContractId,
            BranchId = lead.BranchId,
            BranchName = branch == null ? null : branch.Name,
            ManagerId = lead.ManagerId,
            ManagerLastName = manager == null ? null : manager.FullName.LastName,
            ManagerFirstName = manager == null ? null : manager.FullName.FirstName,
            ManagerMiddleName = manager == null ? null : manager.FullName.MiddleName,
            CreatedAtUtc = lead.CreatedAtUtc
        };

    /// <summary>The columns of a lead that the read models are built from.</summary>
    private sealed class LeadRow
    {
        public Guid Id { get; init; }

        public string ContactName { get; init; } = null!;

        public PhoneNumber? Phone { get; init; }

        public Email? Email { get; init; }

        public LeadSource Source { get; init; }

        public LicenseCategory? DesiredCategory { get; init; }

        public string? Comment { get; init; }

        public LeadStatus Status { get; init; }

        public string? LostReason { get; init; }

        public Guid? ContractId { get; init; }

        public Guid? BranchId { get; init; }

        public string? BranchName { get; init; }

        public Guid? ManagerId { get; init; }

        public string? ManagerLastName { get; init; }

        public string? ManagerFirstName { get; init; }

        public string? ManagerMiddleName { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}

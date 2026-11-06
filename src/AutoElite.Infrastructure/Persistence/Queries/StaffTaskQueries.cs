using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StaffTasks;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads staff tasks with the names of their assignee and author, without tracking.</summary>
internal sealed class StaffTaskQueries(AutoEliteDbContext dbContext) : IStaffTaskQueries
{
    /// <inheritdoc />
    public async Task<PagedList<StaffTaskListItem>> GetListAsync(
        StaffTaskListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Rows();
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.AssigneeBranchId == null || row.AssigneeBranchId == scope.BranchId);

        if (filter.AssigneeId is { } assigneeId)
            rows = rows.Where(row => row.AssigneeId == assigneeId);

        if (filter.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        if (filter.OverdueBeforeUtc is { } overdueBeforeUtc)
            rows = rows.Where(row => row.Status != StaffTaskStatus.Done && row.DueAtUtc < overdueBeforeUtc);

        if (filter.Subject is { } subject)
        {
            var (kind, id) = (subject.Kind, subject.Id);
            rows = rows.Where(row => row.SubjectId == id && row.SubjectKind == kind);
        }

        var (found, totalCount) = await rows
            .OrderBy(row => row.DueAtUtc)
            .ThenBy(row => row.Id)
            .ReadPageAsync(page, cancellationToken);

        return new PagedList<StaffTaskListItem>(
            [
                .. found.Select(row => new StaffTaskListItem(
                    row.Id, row.Title, row.AssigneeId, AssigneeName(row), row.DueAtUtc, row.Status, row.Recurrence,
                    Subject(row)))
            ],
            page,
            totalCount);
    }

    /// <inheritdoc />
    public async Task<StaffTaskDetails?> GetDetailsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == taskId, cancellationToken);

        return row is null
            ? null
            : new StaffTaskDetails(
                row.Id, row.Title, row.Description, row.AssigneeId, AssigneeName(row), row.AssigneeBranchId, row.DueAtUtc,
                row.Status, row.Recurrence, Subject(row), row.CompletedAtUtc, row.CreatedAtUtc,
                PersonNames.Format(row.AuthorLastName, row.AuthorFirstName, row.AuthorMiddleName));
    }

    private static string AssigneeName(TaskRow row) =>
        PersonNames.Format(row.AssigneeLastName, row.AssigneeFirstName, row.AssigneeMiddleName)!;

    /// <summary>Rebuilds the subject from its columns, which are both empty when there is none.</summary>
    private static EntityReference? Subject(TaskRow row) =>
        row is { SubjectKind: { } kind, SubjectId: { } id } ? EntityReference.Create(kind, id).Value : null;

    /// <summary>The tasks with their assignee and author; the subject is read as nullable columns.</summary>
    private IQueryable<TaskRow> Rows() =>
        from task in dbContext.Set<StaffTask>().AsNoTracking()
        join assignee in dbContext.Set<User>() on task.AssigneeId equals assignee.Id
        join author in dbContext.Set<User>() on task.CreatedByUserId equals (Guid?)author.Id into authors
        from author in authors.DefaultIfEmpty()
        select new TaskRow
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssigneeId = task.AssigneeId,
            AssigneeLastName = assignee.FullName.LastName,
            AssigneeFirstName = assignee.FullName.FirstName,
            AssigneeMiddleName = assignee.FullName.MiddleName,
            AssigneeBranchId = assignee.BranchId,
            DueAtUtc = task.DueAtUtc,
            Status = task.Status,
            Recurrence = task.Recurrence,
            SubjectKind = (EntityKind?)task.Subject!.Kind,
            SubjectId = (Guid?)task.Subject!.Id,
            CompletedAtUtc = task.CompletedAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            AuthorLastName = author == null ? null : author.FullName.LastName,
            AuthorFirstName = author == null ? null : author.FullName.FirstName,
            AuthorMiddleName = author == null ? null : author.FullName.MiddleName
        };

    /// <summary>The columns of a task that the read models are built from.</summary>
    private sealed class TaskRow
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = null!;

        public string? Description { get; init; }

        public Guid AssigneeId { get; init; }

        public string AssigneeLastName { get; init; } = null!;

        public string AssigneeFirstName { get; init; } = null!;

        public string? AssigneeMiddleName { get; init; }

        public Guid? AssigneeBranchId { get; init; }

        public DateTime DueAtUtc { get; init; }

        public StaffTaskStatus Status { get; init; }

        public TaskRecurrence Recurrence { get; init; }

        public EntityKind? SubjectKind { get; init; }

        public Guid? SubjectId { get; init; }

        public DateTime? CompletedAtUtc { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string? AuthorLastName { get; init; }

        public string? AuthorFirstName { get; init; }

        public string? AuthorMiddleName { get; init; }
    }
}

using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Identity;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads users with their branch, roles and account state, straight from the tables, without tracking.</summary>
internal sealed class UserQueries(AutoEliteDbContext dbContext) : IUserQueries
{
    /// <inheritdoc />
    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await Rows(Users().Where(user => user.Id == userId)).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new UserProfile(
                row.Id, row.LastName, row.FirstName, row.MiddleName, row.Email.Value, row.Phone?.Value, row.PhotoDocumentId,
                row.BranchId, row.BranchName, row.PreferredLanguage, row.Roles);
    }

    /// <inheritdoc />
    public async Task<UserDetails?> GetDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await Rows(Users().Where(user => user.Id == userId)).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new UserDetails(
                row.Id, row.LastName, row.FirstName, row.MiddleName, row.Email.Value, row.Phone?.Value, row.PhotoDocumentId,
                row.BranchId, row.BranchName, row.PreferredLanguage, row.IsActive, row.HasAcceptedInvitation,
                row.CreatedAtUtc, row.Roles);
    }

    /// <inheritdoc />
    public async Task<PagedList<UserListItem>> GetListAsync(
        UserListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var users = Users();
        if (!scope.IsSchoolWide)
            users = users.Where(user => user.BranchId == null || user.BranchId == scope.BranchId);

        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
        {
            // The email column holds the address as text; the casts let SQL search it although the property is a
            // value object.
            users = users.Where(user =>
                EF.Functions.ILike(user.FullName.LastName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(user.FullName.FirstName, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike(user.FullName.MiddleName!, pattern, QueryExtensions.LikeEscape)
                || EF.Functions.ILike((string)(object)user.Email, pattern, QueryExtensions.LikeEscape));
        }

        if (filter.Role is { } role)
        {
            users = users.Where(user => dbContext.Set<UserRoleAssignment>()
                .Any(assignment => assignment.UserId == user.Id && assignment.Role == role));
        }

        if (filter.BranchId is { } branchId)
            users = users.Where(user => user.BranchId == branchId);

        if (filter.IsActive is { } isActive)
            users = users.Where(user => user.IsActive == isActive);

        var ordered = Rows(users)
            .OrderBy(row => row.LastName)
            .ThenBy(row => row.FirstName)
            .ThenBy(row => row.MiddleName)
            .ThenBy(row => row.Email)
            .ThenBy(row => row.Id);
        var (rows, totalCount) = await ordered.ReadPageAsync(page, cancellationToken);
        UserListItem[] items =
        [
            .. rows.Select(row => new UserListItem(
                row.Id, row.LastName, row.FirstName, row.MiddleName, row.Email.Value, row.Phone?.Value, row.BranchId,
                row.BranchName, row.IsActive, row.HasAcceptedInvitation, row.Roles))
        ];

        return new PagedList<UserListItem>(items, page, totalCount);
    }

    private IQueryable<User> Users() => dbContext.Set<User>().AsNoTracking();

    private IQueryable<UserRow> Rows(IQueryable<User> users) =>
        from user in users
        join branch in dbContext.Set<Branch>() on user.BranchId equals (Guid?)branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        select new UserRow
        {
            Id = user.Id,
            LastName = user.FullName.LastName,
            FirstName = user.FullName.FirstName,
            MiddleName = user.FullName.MiddleName,
            Email = user.Email,
            Phone = user.Phone,
            PhotoDocumentId = user.PhotoDocumentId,
            BranchId = user.BranchId,
            BranchName = branch == null ? null : branch.Name,
            PreferredLanguage = user.PreferredLanguage,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            HasAcceptedInvitation = dbContext.Set<UserAccount>()
                .Any(account => account.Id == user.Id && account.PasswordHash != null),
            Roles = dbContext.Set<UserRoleAssignment>()
                .Where(assignment => assignment.UserId == user.Id)
                .Select(assignment => assignment.Role)
                .ToList()
        };

    /// <summary>The columns of a user that the read models are built from.</summary>
    private sealed class UserRow
    {
        public Guid Id { get; init; }

        public string LastName { get; init; } = null!;

        public string FirstName { get; init; } = null!;

        public string? MiddleName { get; init; }

        public Email Email { get; init; } = null!;

        public PhoneNumber? Phone { get; init; }

        public Guid? PhotoDocumentId { get; init; }

        public Guid? BranchId { get; init; }

        public string? BranchName { get; init; }

        public Language PreferredLanguage { get; init; }

        public bool IsActive { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public bool HasAcceptedInvitation { get; init; }

        public List<UserRole> Roles { get; init; } = null!;
    }
}

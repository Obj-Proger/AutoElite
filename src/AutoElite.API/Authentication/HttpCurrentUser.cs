using System.Security.Claims;
using AutoElite.Infrastructure.Authentication;

namespace AutoElite.API.Authentication;

/// <summary>
/// The caller of the current HTTP request, read from the claims of its access token. Outside a request, in the outbox
/// handlers, the scheduled jobs and the other background work of the host, there is no HTTP context, and the caller is
/// the system itself, as <see cref="ICurrentUser"/> requires.
/// </summary>
internal sealed class HttpCurrentUser : ICurrentUser
{
    private static readonly IReadOnlySet<UserRole> _noRoles = new HashSet<UserRole>();

    /// <summary>Initializes a new instance of the <see cref="HttpCurrentUser"/> class for the scope being created.</summary>
    /// <param name="httpContextAccessor">The accessor of the HTTP context of the request, if any.</param>
    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        IsSystem = context is null;
        Roles = _noRoles;

        var user = context?.User;
        if (user?.Identity?.IsAuthenticated != true || !Guid.TryParse(user.FindFirstValue(AccessTokenClaims.UserId), out var userId))
            return;

        UserId = userId;
        BranchId = Guid.TryParse(user.FindFirstValue(AccessTokenClaims.BranchId), out var branchId) ? branchId : null;
        Roles = user.FindAll(AccessTokenClaims.Role)
            .Select(claim => Enum.TryParse<UserRole>(claim.Value, out var role) && Enum.IsDefined(role) ? role : (UserRole?)null)
            .OfType<UserRole>()
            .ToHashSet();
    }

    /// <inheritdoc />
    public Guid? UserId { get; }

    /// <inheritdoc />
    public Guid? BranchId { get; }

    /// <inheritdoc />
    public IReadOnlySet<UserRole> Roles { get; }

    /// <inheritdoc />
    public bool IsSystem { get; }
}

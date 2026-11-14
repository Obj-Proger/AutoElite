namespace AutoElite.API.DemoData;

/// <summary>A demo user as the caller of the commands that create the demo data on their behalf.</summary>
/// <param name="UserId">The user.</param>
/// <param name="BranchId">The branch of the user, if any.</param>
/// <param name="Role">The role the user acts in.</param>
internal sealed record DemoCaller(Guid UserId, Guid? BranchId, UserRole Role) : ICurrentUser
{
    /// <inheritdoc />
    Guid? ICurrentUser.UserId => UserId;

    /// <inheritdoc />
    public IReadOnlySet<UserRole> Roles { get; } = new HashSet<UserRole> { Role };

    /// <inheritdoc />
    public bool IsSystem => false;
}

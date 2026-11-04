namespace AutoElite.Application.Common.Authorization;

/// <summary>
/// The branches whose records the current user may see. A manager tied to a branch sees only that branch;
/// administrators and users not tied to a branch see the whole school.
/// </summary>
/// <remarks>
/// Records not tied to any branch belong to the school as a whole and are visible in every scope.
/// A record outside the scope is reported as not found, so that its existence is not revealed.
/// Ownership ("my lessons", "my groups") is a separate check made by the handler.
/// </remarks>
public sealed record DataScope
{
    private DataScope(Guid? branchId)
    {
        BranchId = branchId;
    }

    /// <summary>Gets a scope that covers every branch.</summary>
    public static DataScope SchoolWide { get; } = new((Guid?)null);

    /// <summary>Gets the branch the scope is limited to, or <see langword="null"/> for the whole school.</summary>
    public Guid? BranchId { get; }

    /// <summary>Gets a value indicating whether the scope covers every branch.</summary>
    public bool IsSchoolWide => BranchId is null;

    /// <summary>Creates a scope limited to one branch.</summary>
    /// <param name="branchId">The identifier of the branch.</param>
    public static DataScope ForBranch(Guid branchId)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("The branch identifier must not be empty.", nameof(branchId));

        return new DataScope(branchId);
    }

    /// <summary>Determines the scope of a user.</summary>
    /// <param name="currentUser">The user on whose behalf the request runs.</param>
    public static DataScope For(ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (currentUser.IsInRole(UserRole.Administrator) || currentUser.BranchId is not { } branchId)
            return SchoolWide;

        return ForBranch(branchId);
    }

    /// <summary>Determines whether a record tied to the given branch is inside the scope.</summary>
    /// <param name="recordBranchId">The branch of the record, or <see langword="null"/> if it is not tied to one.</param>
    public bool Includes(Guid? recordBranchId) =>
        IsSchoolWide || recordBranchId is null || recordBranchId == BranchId;
}

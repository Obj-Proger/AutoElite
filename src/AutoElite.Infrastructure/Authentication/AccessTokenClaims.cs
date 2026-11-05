namespace AutoElite.Infrastructure.Authentication;

/// <summary>The claims an access token carries; the host reads the current user from them.</summary>
public static class AccessTokenClaims
{
    /// <summary>The identifier of the user (the registered <c>sub</c> claim).</summary>
    public const string UserId = "sub";

    /// <summary>The branch of the user, absent for users not tied to a branch.</summary>
    public const string BranchId = "branch";

    /// <summary>A role of the user, once per role.</summary>
    public const string Role = "role";
}

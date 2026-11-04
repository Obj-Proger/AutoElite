namespace AutoElite.Application.Users;

/// <summary>The conditions of the user list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Search">Text to find in the last, first or middle name or the email address, case-insensitively.</param>
/// <param name="Role">Only users who hold this role.</param>
/// <param name="BranchId">Only users of this branch.</param>
/// <param name="IsActive">Only active or only deactivated users.</param>
public sealed record UserListFilter(string? Search, UserRole? Role, Guid? BranchId, bool? IsActive);

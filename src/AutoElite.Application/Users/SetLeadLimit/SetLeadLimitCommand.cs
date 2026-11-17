namespace AutoElite.Application.Users.SetLeadLimit;

/// <summary>Sets how many leads a manager may work on at a time; an administrator may give them more.</summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="LeadLimit">The number of leads in progress, from 1 to <see cref="User.MaxLeadLimit"/>.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record SetLeadLimitCommand(Guid UserId, int LeadLimit) : ICommand;

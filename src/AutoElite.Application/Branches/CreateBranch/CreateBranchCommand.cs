namespace AutoElite.Application.Branches.CreateBranch;

/// <summary>Opens a new branch (FR-1.8).</summary>
/// <param name="Name">The name, unique within the school.</param>
/// <param name="City">The city.</param>
/// <param name="Street">The street, building and office.</param>
/// <param name="Phone">The contact phone number in E.164 format, if any.</param>
/// <param name="TimeZoneId">The IANA time zone identifier, for example <c>Asia/Yekaterinburg</c>.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record CreateBranchCommand(
    string? Name,
    string? City,
    string? Street,
    string? Phone,
    string? TimeZoneId) : ICommand<Guid>;

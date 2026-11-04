namespace AutoElite.Application.Branches.UpdateBranch;

/// <summary>Changes the details of an active branch.</summary>
/// <param name="BranchId">The identifier of the branch.</param>
/// <param name="Name">The name, unique within the school.</param>
/// <param name="City">The city.</param>
/// <param name="Street">The street, building and office.</param>
/// <param name="Phone">The contact phone number in E.164 format; empty removes it.</param>
/// <param name="TimeZoneId">The IANA time zone identifier.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateBranchCommand(
    Guid BranchId,
    string? Name,
    string? City,
    string? Street,
    string? Phone,
    string? TimeZoneId) : ICommand;

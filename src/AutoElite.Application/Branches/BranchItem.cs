namespace AutoElite.Application.Branches;

/// <summary>A branch as lists and forms show it.</summary>
/// <param name="Id">The identifier of the branch.</param>
/// <param name="Name">The name.</param>
/// <param name="City">The city.</param>
/// <param name="Street">The street and building.</param>
/// <param name="Phone">The contact phone number, if any.</param>
/// <param name="TimeZoneId">The IANA time zone identifier.</param>
/// <param name="IsActive">Whether the branch is active.</param>
public sealed record BranchItem(
    Guid Id,
    string Name,
    string City,
    string Street,
    string? Phone,
    string TimeZoneId,
    bool IsActive);

namespace AutoElite.Application.Leads;

/// <summary>The conditions of the lead list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Search">Text to find in the contact name, phone or email, case-insensitively.</param>
/// <param name="Status">Only leads in this status.</param>
/// <param name="Source">Only leads from this channel.</param>
/// <param name="ManagerId">Only leads of this manager.</param>
/// <param name="BranchId">Only leads of this branch.</param>
public sealed record LeadListFilter(string? Search, LeadStatus? Status, LeadSource? Source, Guid? ManagerId, Guid? BranchId);

/// <summary>A row of the lead list.</summary>
/// <param name="Id">The identifier of the lead.</param>
/// <param name="ContactName">The name the person gave.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="Email">The email address, if any.</param>
/// <param name="Source">The channel through which the lead arrived.</param>
/// <param name="DesiredCategory">The licence category of interest, if known.</param>
/// <param name="Status">The funnel status.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="ManagerId">The responsible manager, if any.</param>
/// <param name="ManagerName">The full name of the manager, if any.</param>
/// <param name="CreatedAtUtc">When the lead arrived.</param>
public sealed record LeadListItem(
    Guid Id,
    string ContactName,
    string? Phone,
    string? Email,
    LeadSource Source,
    LicenseCategory? DesiredCategory,
    LeadStatus Status,
    Guid? BranchId,
    string? BranchName,
    Guid? ManagerId,
    string? ManagerName,
    DateTime CreatedAtUtc);

/// <summary>A note of the lead journal.</summary>
/// <param name="Id">The identifier of the note.</param>
/// <param name="Text">The text.</param>
/// <param name="AuthorId">The staff member who wrote it.</param>
/// <param name="AuthorName">The full name of the author.</param>
/// <param name="WrittenAtUtc">When it was written.</param>
public sealed record LeadNoteItem(Guid Id, string Text, Guid AuthorId, string AuthorName, DateTime WrittenAtUtc);

/// <summary>A transition of the lead funnel.</summary>
/// <param name="FromStatus">The status before, or <see langword="null"/> for the creation.</param>
/// <param name="ToStatus">The status after.</param>
/// <param name="ChangedAtUtc">When the status changed.</param>
/// <param name="ChangedByUserId">The user who changed it, or <see langword="null"/> for the public form or the system.</param>
/// <param name="ChangedByName">The full name of that user, if any.</param>
/// <param name="Reason">The reason, for a lost lead.</param>
public sealed record LeadStatusChangeItem(
    LeadStatus? FromStatus,
    LeadStatus ToStatus,
    DateTime ChangedAtUtc,
    Guid? ChangedByUserId,
    string? ChangedByName,
    string? Reason);

/// <summary>The card of a lead with its journal and status history.</summary>
/// <param name="Id">The identifier of the lead.</param>
/// <param name="ContactName">The name the person gave.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="Email">The email address, if any.</param>
/// <param name="Source">The channel through which the lead arrived.</param>
/// <param name="DesiredCategory">The licence category of interest, if known.</param>
/// <param name="Comment">The comment, if any.</param>
/// <param name="Status">The funnel status.</param>
/// <param name="LostReason">The reason the lead was lost, if it is lost.</param>
/// <param name="ContractId">The contract the lead was converted into, if any.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="ManagerId">The responsible manager, if any.</param>
/// <param name="ManagerName">The full name of the manager, if any.</param>
/// <param name="CreatedAtUtc">When the lead arrived.</param>
/// <param name="Notes">The notes, oldest first.</param>
/// <param name="StatusHistory">The status transitions, oldest first.</param>
public sealed record LeadDetails(
    Guid Id,
    string ContactName,
    string? Phone,
    string? Email,
    LeadSource Source,
    LicenseCategory? DesiredCategory,
    string? Comment,
    LeadStatus Status,
    string? LostReason,
    Guid? ContractId,
    Guid? BranchId,
    string? BranchName,
    Guid? ManagerId,
    string? ManagerName,
    DateTime CreatedAtUtc,
    IReadOnlyList<LeadNoteItem> Notes,
    IReadOnlyList<LeadStatusChangeItem> StatusHistory);

namespace AutoElite.Application.Leads.AddLeadNote;

/// <summary>Adds a note to the lead journal; notes can be added at any stage (FR-2.5).</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="Text">The note text. Required.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record AddLeadNoteCommand(Guid LeadId, string? Text) : ICommand<Guid>;

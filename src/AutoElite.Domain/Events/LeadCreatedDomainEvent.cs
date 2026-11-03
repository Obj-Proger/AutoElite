using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>Raised when a lead is recorded; managers are notified about new requests.</summary>
/// <param name="LeadId">The identifier of the new lead.</param>
/// <param name="BranchId">The branch handling the lead, if known.</param>
/// <param name="Source">The channel through which the lead arrived.</param>
/// <param name="OccurredOnUtc">The UTC time at which the lead was recorded.</param>
public sealed record LeadCreatedDomainEvent(Guid LeadId, Guid? BranchId, LeadSource Source, DateTime OccurredOnUtc) : IDomainEvent;

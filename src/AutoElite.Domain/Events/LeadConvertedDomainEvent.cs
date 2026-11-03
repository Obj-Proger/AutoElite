namespace AutoElite.Domain.Events;

/// <summary>Raised when a lead is converted into a contract.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="ContractId">The identifier of the contract the lead was converted into.</param>
/// <param name="OccurredOnUtc">The UTC time of the conversion.</param>
public sealed record LeadConvertedDomainEvent(Guid LeadId, Guid ContractId, DateTime OccurredOnUtc) : IDomainEvent;

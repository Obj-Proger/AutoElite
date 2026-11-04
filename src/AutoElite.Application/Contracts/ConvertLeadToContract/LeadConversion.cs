namespace AutoElite.Application.Contracts.ConvertLeadToContract;

/// <summary>The result of converting a lead.</summary>
/// <param name="StudentId">The student of the contract, new or existing.</param>
/// <param name="ContractId">The new contract.</param>
public sealed record LeadConversion(Guid StudentId, Guid ContractId);

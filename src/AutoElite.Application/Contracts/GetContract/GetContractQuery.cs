namespace AutoElite.Application.Contracts.GetContract;

/// <summary>
/// Reads the card of a contract. Staff see the contracts of their scope; a student sees only their own contracts.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.Student)]
public sealed record GetContractQuery(Guid ContractId) : IQuery<ContractDetails>;

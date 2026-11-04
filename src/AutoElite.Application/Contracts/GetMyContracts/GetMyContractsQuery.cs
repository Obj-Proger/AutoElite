namespace AutoElite.Application.Contracts.GetMyContracts;

/// <summary>Reads the contracts of the signed-in student, the latest start first.</summary>
[RequireRoles(UserRole.Student)]
public sealed record GetMyContractsQuery : IQuery<IReadOnlyList<ContractListItem>>;

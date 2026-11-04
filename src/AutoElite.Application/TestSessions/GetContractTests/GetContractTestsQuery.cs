namespace AutoElite.Application.TestSessions.GetContractTests;

/// <summary>
/// Reads the history of all test attempts of a contract, the latest first (FR-7.8). The student reads their own
/// contracts; managers read the contracts in their scope.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
[RequireRoles(UserRole.Student, UserRole.Manager, UserRole.Administrator)]
public sealed record GetContractTestsQuery(Guid ContractId) : IQuery<IReadOnlyList<TestSessionItem>>;

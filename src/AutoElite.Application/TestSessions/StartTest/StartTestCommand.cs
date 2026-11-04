namespace AutoElite.Application.TestSessions.StartTest;

/// <summary>
/// Starts a practice test of the signed-in student on a contract from an active template (FR-7.5): questions are
/// drawn at random for each block and the timer starts. A contract has one attempt in progress at a time.
/// </summary>
/// <param name="ContractId">An active contract of the student.</param>
/// <param name="TemplateId">An active template for the category of the contract.</param>
[RequireRoles(UserRole.Student)]
public sealed record StartTestCommand(Guid ContractId, Guid TemplateId) : ICommand<Guid>;

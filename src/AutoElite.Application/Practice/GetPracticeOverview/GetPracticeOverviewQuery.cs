namespace AutoElite.Application.Practice.GetPracticeOverview;

/// <summary>
/// Reads how far the signed-in student has gone through the questions of the licence category of a contract: the
/// questions answered in practice and the mistakes, in all and by topic.
/// </summary>
/// <param name="ContractId">A contract of the student.</param>
[RequireRoles(UserRole.Student)]
public sealed record GetPracticeOverviewQuery(Guid ContractId) : IQuery<PracticeOverview>;

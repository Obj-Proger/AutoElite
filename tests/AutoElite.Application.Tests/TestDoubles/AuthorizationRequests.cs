using AutoElite.Application.Common.Authorization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

[AllowAnonymousRequest]
internal sealed record PublicCommand : ICommand;

[RequireRoles(UserRole.Manager, UserRole.Administrator)]
internal sealed record ManagerCommand : ICommand;

[RequireRoles(UserRole.Administrator)]
internal sealed record AdministratorQuery : IQuery<int>;

[AllowAnonymousRequest]
[RequireRoles(UserRole.Manager)]
internal sealed record ContradictoryCommand : ICommand;

[SystemRequest]
internal sealed record SystemCommand : ICommand;

[SystemRequest]
[RequireRoles(UserRole.Administrator)]
internal sealed record SystemAndRolesCommand : ICommand;

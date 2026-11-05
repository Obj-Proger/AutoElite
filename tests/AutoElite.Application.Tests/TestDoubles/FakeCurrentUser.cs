using AutoElite.Application.Common.Abstractions;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; }

    public Guid? BranchId { get; init; }

    public IReadOnlySet<UserRole> Roles { get; init; } = new HashSet<UserRole>();

    public bool IsSystem { get; init; }

    public static FakeCurrentUser Anonymous() => new();

    public static FakeCurrentUser System() => new() { IsSystem = true };

    public static FakeCurrentUser SignedIn(params UserRole[] roles) =>
        new() { UserId = Guid.CreateVersion7(), Roles = roles.ToHashSet() };
}

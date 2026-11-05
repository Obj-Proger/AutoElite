using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Tests.TestDoubles;

/// <summary>The user a test acts as.</summary>
internal sealed class TestCurrentUser : ICurrentUser
{
    /// <summary>Gets the system, which has no user identifier.</summary>
    public static TestCurrentUser System { get; } = new() { IsSystem = true };

    /// <inheritdoc />
    public Guid? UserId { get; init; }

    /// <inheritdoc />
    public Guid? BranchId { get; init; }

    /// <inheritdoc />
    public IReadOnlySet<UserRole> Roles { get; init; } = new HashSet<UserRole>();

    /// <inheritdoc />
    public bool IsSystem { get; init; }

    /// <summary>Creates a signed-in administrator with a new identifier.</summary>
    public static TestCurrentUser Administrator() =>
        new() { UserId = Guid.CreateVersion7(), Roles = new HashSet<UserRole> { UserRole.Administrator } };
}

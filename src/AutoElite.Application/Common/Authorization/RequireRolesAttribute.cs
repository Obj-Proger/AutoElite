namespace AutoElite.Application.Common.Authorization;

/// <summary>
/// Restricts a command or query to users who hold at least one of the listed roles.
/// Without this attribute a request is still open only to signed-in users, unless it is marked with
/// <see cref="AllowAnonymousRequestAttribute"/>.
/// </summary>
/// <remarks>
/// The attribute checks only the role. Whether the record belongs to the user or to the manager's branch
/// is checked by the handler against the loaded resource, using <see cref="DataScope"/>.
/// </remarks>
/// <param name="roles">The roles allowed to send the request; the user needs any one of them.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequireRolesAttribute(params UserRole[] roles) : Attribute
{
    /// <summary>Gets the roles allowed to send the request.</summary>
    public IReadOnlyList<UserRole> Roles { get; } = roles;
}

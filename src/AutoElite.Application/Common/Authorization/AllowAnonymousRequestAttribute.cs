namespace AutoElite.Application.Common.Authorization;

/// <summary>
/// Opens a command or query to anonymous callers, for example signing in or accepting an invitation.
/// Every other request requires a signed-in user.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AllowAnonymousRequestAttribute : Attribute;

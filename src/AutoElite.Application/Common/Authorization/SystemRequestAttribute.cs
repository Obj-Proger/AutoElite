namespace AutoElite.Application.Common.Authorization;

/// <summary>
/// Reserves a command or query for the system itself, for example a scheduled job that sends lesson reminders.
/// Only a caller whose <see cref="ICurrentUser.IsSystem"/> is set may send it; users never can, whatever their roles.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SystemRequestAttribute : Attribute;

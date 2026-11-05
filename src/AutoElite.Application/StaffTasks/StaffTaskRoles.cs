namespace AutoElite.Application.StaffTasks;

/// <summary>The roles of the staff members who can be given tasks and work on them (M-12).</summary>
internal static class StaffTaskRoles
{
    /// <summary>Gets the roles of staff members; students cannot be given tasks.</summary>
    public static IReadOnlyList<UserRole> Staff { get; } =
        [UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor];
}

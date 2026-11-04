namespace AutoElite.Application.Exams;

/// <summary>
/// Decides who reads the exams of a group: the staff and the driving instructors who see the branch of the group, and
/// the responsible theory instructor of the group.
/// </summary>
internal static class ExamReaders
{
    /// <summary>Determines whether the caller may read the exams of a group.</summary>
    /// <param name="currentUser">The caller.</param>
    /// <param name="branchId">The branch of the group, if any.</param>
    /// <param name="responsibleInstructorId">The responsible theory instructor of the group.</param>
    public static bool MayRead(ICurrentUser currentUser, Guid? branchId, Guid responsibleInstructorId)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor)
            && DataScope.For(currentUser).Includes(branchId))
            return true;

        return responsibleInstructorId == currentUser.UserId;
    }
}

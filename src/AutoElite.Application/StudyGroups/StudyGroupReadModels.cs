namespace AutoElite.Application.StudyGroups;

/// <summary>The conditions of the group list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Search">Text to find in the group name, case-insensitively.</param>
/// <param name="CourseId">Only groups of this course.</param>
/// <param name="TheoryInstructorId">Only groups of this instructor.</param>
/// <param name="BranchId">Only groups of this branch.</param>
/// <param name="IsActive">Only active or only closed groups.</param>
public sealed record StudyGroupListFilter(
    string? Search,
    Guid? CourseId,
    Guid? TheoryInstructorId,
    Guid? BranchId,
    bool? IsActive);

/// <summary>A row of the group list.</summary>
/// <param name="Id">The identifier of the group.</param>
/// <param name="Name">The name.</param>
/// <param name="CourseId">The course.</param>
/// <param name="CourseName">The name of the course.</param>
/// <param name="Category">The licence category.</param>
/// <param name="TheoryInstructorId">The responsible theory instructor.</param>
/// <param name="TheoryInstructorName">The full name of the instructor.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training.</param>
/// <param name="Capacity">The maximum number of students.</param>
/// <param name="CurrentMemberCount">The number of current students.</param>
/// <param name="IsActive">Whether the group is active.</param>
public sealed record StudyGroupListItem(
    Guid Id,
    string Name,
    Guid CourseId,
    string CourseName,
    LicenseCategory Category,
    Guid TheoryInstructorId,
    string TheoryInstructorName,
    Guid? BranchId,
    string? BranchName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Capacity,
    int CurrentMemberCount,
    bool IsActive);

/// <summary>An enrollment of a student in a group.</summary>
/// <param name="EnrollmentId">The identifier of the enrollment.</param>
/// <param name="ContractId">The contract.</param>
/// <param name="ContractNumber">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="EnrolledAtUtc">When the student joined.</param>
/// <param name="WithdrawnAtUtc">When the student left, or <see langword="null"/> for a current member.</param>
public sealed record StudyGroupMemberItem(
    Guid EnrollmentId,
    Guid ContractId,
    string ContractNumber,
    Guid StudentId,
    string StudentName,
    DateTime EnrolledAtUtc,
    DateTime? WithdrawnAtUtc);

/// <summary>The card of a group with its members.</summary>
/// <param name="Id">The identifier of the group.</param>
/// <param name="Name">The name.</param>
/// <param name="CourseId">The course.</param>
/// <param name="CourseName">The name of the course.</param>
/// <param name="Category">The licence category.</param>
/// <param name="TheoryInstructorId">The responsible theory instructor.</param>
/// <param name="TheoryInstructorName">The full name of the instructor.</param>
/// <param name="BranchId">The branch, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training.</param>
/// <param name="Capacity">The maximum number of students.</param>
/// <param name="IsActive">Whether the group is active.</param>
/// <param name="Enrollments">All enrollments, current and past, in the order they were made.</param>
public sealed record StudyGroupDetails(
    Guid Id,
    string Name,
    Guid CourseId,
    string CourseName,
    LicenseCategory Category,
    Guid TheoryInstructorId,
    string TheoryInstructorName,
    Guid? BranchId,
    string? BranchName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Capacity,
    bool IsActive,
    IReadOnlyList<StudyGroupMemberItem> Enrollments);

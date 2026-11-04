namespace AutoElite.Application.TheoryLessons;

/// <summary>The conditions of the theory schedule; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Period">Only lessons that overlap this time.</param>
/// <param name="InstructorId">Only lessons the instructor gives or whose group the instructor is responsible for.</param>
/// <param name="GroupId">Only lessons of this group.</param>
/// <param name="BranchId">Only lessons of groups of this branch.</param>
/// <param name="Status">Only lessons in this status.</param>
public sealed record TheoryScheduleFilter(
    TimeInterval Period,
    Guid? InstructorId,
    Guid? GroupId,
    Guid? BranchId,
    TheoryLessonStatus? Status);

/// <summary>A lesson in a schedule or a list.</summary>
/// <param name="Id">The identifier of the lesson.</param>
/// <param name="GroupId">The study group.</param>
/// <param name="GroupName">The name of the group.</param>
/// <param name="BranchId">The branch of the group, if any.</param>
/// <param name="TopicId">The course topic.</param>
/// <param name="TopicTitle">The title of the topic.</param>
/// <param name="InstructorId">The instructor giving the lesson.</param>
/// <param name="InstructorName">The full name of the instructor.</param>
/// <param name="StartUtc">The start in UTC.</param>
/// <param name="EndUtc">The end in UTC.</param>
/// <param name="Status">The lesson status.</param>
public sealed record TheoryLessonItem(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid? BranchId,
    Guid TopicId,
    string TopicTitle,
    Guid InstructorId,
    string InstructorName,
    DateTime StartUtc,
    DateTime EndUtc,
    TheoryLessonStatus Status);

/// <summary>A lesson in the schedule of a student, with the student's own attendance.</summary>
/// <param name="Lesson">The lesson.</param>
/// <param name="IsPresent">Whether the student attended, or <see langword="null"/> if attendance is not marked yet.</param>
public sealed record StudentTheoryLessonItem(TheoryLessonItem Lesson, bool? IsPresent);

/// <summary>The lessons of a group with what is needed to decide who may read them.</summary>
/// <param name="BranchId">The branch of the group, if any.</param>
/// <param name="TheoryInstructorId">The responsible instructor of the group.</param>
/// <param name="Lessons">All lessons of the group, cancelled ones included, the earliest first.</param>
public sealed record GroupTheoryLessons(Guid? BranchId, Guid TheoryInstructorId, IReadOnlyList<TheoryLessonItem> Lessons);

/// <summary>A student expected at a lesson, with the attendance mark if one has been made.</summary>
/// <param name="ContractId">The contract.</param>
/// <param name="ContractNumber">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="IsPresent">Whether the student attended, or <see langword="null"/> if not marked yet.</param>
/// <param name="Comment">The comment of the mark, if any.</param>
/// <param name="MarkedAtUtc">When the mark was last made, if it was.</param>
public sealed record LessonParticipantItem(
    Guid ContractId,
    string ContractNumber,
    Guid StudentId,
    string StudentName,
    bool? IsPresent,
    string? Comment,
    DateTime? MarkedAtUtc);

/// <summary>The card of a lesson with its attendance.</summary>
/// <param name="Id">The identifier of the lesson.</param>
/// <param name="GroupId">The study group.</param>
/// <param name="GroupName">The name of the group.</param>
/// <param name="BranchId">The branch of the group, if any.</param>
/// <param name="ResponsibleInstructorId">The responsible instructor of the group.</param>
/// <param name="TopicId">The course topic.</param>
/// <param name="TopicTitle">The title of the topic.</param>
/// <param name="InstructorId">The instructor giving the lesson.</param>
/// <param name="InstructorName">The full name of the instructor.</param>
/// <param name="StartUtc">The start in UTC.</param>
/// <param name="EndUtc">The end in UTC.</param>
/// <param name="Status">The lesson status.</param>
/// <param name="CancellationReason">Why the lesson was cancelled, if it was.</param>
/// <param name="Participants">
/// The current members of the group and the students already marked, by student name; together they are the
/// roster the instructor marks.
/// </param>
public sealed record TheoryLessonDetails(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid? BranchId,
    Guid ResponsibleInstructorId,
    Guid TopicId,
    string TopicTitle,
    Guid InstructorId,
    string InstructorName,
    DateTime StartUtc,
    DateTime EndUtc,
    TheoryLessonStatus Status,
    string? CancellationReason,
    IReadOnlyList<LessonParticipantItem> Participants);

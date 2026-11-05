using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TheoryLessons;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads theory lessons with their group, topic, instructor and attendance, without tracking.</summary>
internal sealed class TheoryLessonQueries(AutoEliteDbContext dbContext) : ITheoryLessonQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TheoryLessonItem>> GetScheduleAsync(
        TheoryScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var period = filter.Period;
        var rows = Rows().Where(row => row.StartUtc < period.EndUtc && period.StartUtc < row.EndUtc);
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (filter.InstructorId is { } instructorId)
            rows = rows.Where(row => row.InstructorId == instructorId || row.ResponsibleInstructorId == instructorId);

        if (filter.GroupId is { } groupId)
            rows = rows.Where(row => row.GroupId == groupId);

        if (filter.BranchId is { } branchId)
            rows = rows.Where(row => row.BranchId == branchId);

        if (filter.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        var found = await Earliest(rows).ToListAsync(cancellationToken);

        return [.. found.Select(ToItem)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudentTheoryLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default)
    {
        var enrollments = dbContext.Set<GroupEnrollment>();
        var marks = dbContext.Set<LessonAttendance>();
        var found = await Earliest(Rows()
                .Where(row => row.StartUtc < period.EndUtc && period.StartUtc < row.EndUtc)
                .Where(row =>
                    enrollments.Any(enrollment =>
                        EF.Property<Guid>(enrollment, StudyGroupConfiguration.GroupKey) == row.GroupId
                        && enrollment.StudentId == studentId
                        && enrollment.EnrolledAtUtc <= row.StartUtc
                        && (enrollment.WithdrawnAtUtc == null || row.StartUtc < enrollment.WithdrawnAtUtc))
                    || marks.Any(mark =>
                        EF.Property<Guid>(mark, TheoryLessonConfiguration.LessonKey) == row.Id && mark.StudentId == studentId)))
            .Select(row => new
            {
                Row = row,
                IsPresent = marks
                    .Where(mark => EF.Property<Guid>(mark, TheoryLessonConfiguration.LessonKey) == row.Id
                        && mark.StudentId == studentId)
                    .Select(mark => (bool?)mark.IsPresent)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return [.. found.Select(lesson => new StudentTheoryLessonItem(ToItem(lesson.Row), lesson.IsPresent))];
    }

    /// <inheritdoc />
    public async Task<GroupTheoryLessons?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.Set<StudyGroup>().AsNoTracking()
            .Where(found => found.Id == groupId)
            .Select(found => new { found.BranchId, found.TheoryInstructorId })
            .FirstOrDefaultAsync(cancellationToken);
        if (group is null)
            return null;

        var lessons = await Earliest(Rows().Where(row => row.GroupId == groupId)).ToListAsync(cancellationToken);

        return new GroupTheoryLessons(group.BranchId, group.TheoryInstructorId, [.. lessons.Select(ToItem)]);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The roster is the current members of the group together with the students already marked, who may have left
    /// the group since; each appears once, with the mark if there is one.
    /// </remarks>
    public async Task<TheoryLessonDetails?> GetDetailsAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == lessonId, cancellationToken);
        if (row is null)
            return null;

        var members = await dbContext.Set<GroupEnrollment>().AsNoTracking()
            .Where(enrollment => EF.Property<Guid>(enrollment, StudyGroupConfiguration.GroupKey) == row.GroupId
                && enrollment.WithdrawnAtUtc == null)
            .Select(enrollment => enrollment.ContractId)
            .ToListAsync(cancellationToken);
        var marks = await dbContext.Set<LessonAttendance>().AsNoTracking()
            .Where(mark => EF.Property<Guid>(mark, TheoryLessonConfiguration.LessonKey) == lessonId)
            .ToDictionaryAsync(mark => mark.ContractId, cancellationToken);
        List<Guid> contractIds = [.. members.Union(marks.Keys)];
        var contracts = await (
                from contract in dbContext.Set<Contract>().AsNoTracking()
                join student in dbContext.Set<User>() on contract.StudentId equals student.Id
                where contractIds.Contains(contract.Id)
                select new
                {
                    contract.Id,
                    contract.Number,
                    contract.StudentId,
                    student.FullName.LastName,
                    student.FullName.FirstName,
                    student.FullName.MiddleName
                })
            .ToListAsync(cancellationToken);

        LessonParticipantItem[] participants =
        [
            .. contracts
                .Select(contract => new LessonParticipantItem(
                    contract.Id, contract.Number, contract.StudentId,
                    PersonNames.Format(contract.LastName, contract.FirstName, contract.MiddleName)!,
                    marks.GetValueOrDefault(contract.Id)?.IsPresent, marks.GetValueOrDefault(contract.Id)?.Comment,
                    marks.GetValueOrDefault(contract.Id)?.MarkedAtUtc))
                .OrderBy(participant => participant.StudentName, StringComparer.CurrentCulture)
                .ThenBy(participant => participant.ContractNumber, StringComparer.Ordinal)
        ];

        return new TheoryLessonDetails(
            row.Id, row.GroupId, row.GroupName, row.BranchId, row.ResponsibleInstructorId, row.TopicId, row.TopicTitle,
            row.InstructorId, InstructorName(row), row.StartUtc, row.EndUtc, row.Status, row.CancellationReason,
            participants);
    }

    private static IQueryable<LessonRow> Earliest(IQueryable<LessonRow> rows) =>
        rows.OrderBy(row => row.StartUtc).ThenBy(row => row.Id);

    private static TheoryLessonItem ToItem(LessonRow row) =>
        new(
            row.Id, row.GroupId, row.GroupName, row.BranchId, row.TopicId, row.TopicTitle, row.InstructorId,
            InstructorName(row), row.StartUtc, row.EndUtc, row.Status);

    private static string InstructorName(LessonRow row) =>
        PersonNames.Format(row.InstructorLastName, row.InstructorFirstName, row.InstructorMiddleName)!;

    private IQueryable<LessonRow> Rows() =>
        from lesson in dbContext.Set<TheoryLesson>().AsNoTracking()
        join studyGroup in dbContext.Set<StudyGroup>() on lesson.GroupId equals studyGroup.Id
        join topic in dbContext.Set<CourseTopic>() on lesson.TopicId equals topic.Id
        join instructor in dbContext.Set<User>() on lesson.InstructorId equals instructor.Id
        select new LessonRow
        {
            Id = lesson.Id,
            GroupId = lesson.GroupId,
            GroupName = studyGroup.Name,
            BranchId = studyGroup.BranchId,
            ResponsibleInstructorId = studyGroup.TheoryInstructorId,
            TopicId = lesson.TopicId,
            TopicTitle = topic.Title,
            InstructorId = lesson.InstructorId,
            InstructorLastName = instructor.FullName.LastName,
            InstructorFirstName = instructor.FullName.FirstName,
            InstructorMiddleName = instructor.FullName.MiddleName,
            StartUtc = lesson.Time.StartUtc,
            EndUtc = lesson.Time.EndUtc,
            Status = lesson.Status,
            CancellationReason = lesson.CancellationReason
        };

    /// <summary>The columns of a lesson that the read models are built from.</summary>
    private sealed class LessonRow
    {
        public Guid Id { get; init; }

        public Guid GroupId { get; init; }

        public string GroupName { get; init; } = null!;

        public Guid? BranchId { get; init; }

        public Guid ResponsibleInstructorId { get; init; }

        public Guid TopicId { get; init; }

        public string TopicTitle { get; init; } = null!;

        public Guid InstructorId { get; init; }

        public string InstructorLastName { get; init; } = null!;

        public string InstructorFirstName { get; init; } = null!;

        public string? InstructorMiddleName { get; init; }

        public DateTime StartUtc { get; init; }

        public DateTime EndUtc { get; init; }

        public TheoryLessonStatus Status { get; init; }

        public string? CancellationReason { get; init; }
    }
}

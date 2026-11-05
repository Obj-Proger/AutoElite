using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StudyGroups;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads study groups with their course, instructor, branch and members, without tracking.</summary>
internal sealed class StudyGroupQueries(AutoEliteDbContext dbContext) : IStudyGroupQueries
{
    /// <inheritdoc />
    public async Task<PagedList<StudyGroupListItem>> GetListAsync(
        StudyGroupListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Rows();
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
            rows = rows.Where(row => EF.Functions.ILike(row.Name, pattern, QueryExtensions.LikeEscape));

        if (filter.CourseId is { } courseId)
            rows = rows.Where(row => row.CourseId == courseId);

        if (filter.TheoryInstructorId is { } instructorId)
            rows = rows.Where(row => row.TheoryInstructorId == instructorId);

        if (filter.BranchId is { } branchId)
            rows = rows.Where(row => row.BranchId == branchId);

        if (filter.IsActive is { } isActive)
            rows = rows.Where(row => row.IsActive == isActive);

        var ordered = rows.OrderByDescending(row => row.StartDate).ThenBy(row => row.Name).ThenBy(row => row.Id);
        var (found, totalCount) = await ordered.ReadPageAsync(page, cancellationToken);
        StudyGroupListItem[] items =
        [
            .. found.Select(row => new StudyGroupListItem(
                row.Id, row.Name, row.CourseId, row.CourseName, row.Category, row.TheoryInstructorId, InstructorName(row),
                row.BranchId, row.BranchName, row.StartDate, row.EndDate, row.Capacity, row.CurrentMemberCount, row.IsActive))
        ];

        return new PagedList<StudyGroupListItem>(items, page, totalCount);
    }

    /// <inheritdoc />
    public async Task<StudyGroupDetails?> GetDetailsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == groupId, cancellationToken);
        if (row is null)
            return null;

        var members = await (
                from enrollment in dbContext.Set<GroupEnrollment>().AsNoTracking()
                join contract in dbContext.Set<Contract>() on enrollment.ContractId equals contract.Id
                join student in dbContext.Set<User>() on enrollment.StudentId equals student.Id
                where EF.Property<Guid>(enrollment, StudyGroupConfiguration.GroupKey) == groupId
                orderby enrollment.EnrolledAtUtc, enrollment.Id
                select new
                {
                    enrollment.Id,
                    enrollment.ContractId,
                    contract.Number,
                    enrollment.StudentId,
                    student.FullName.LastName,
                    student.FullName.FirstName,
                    student.FullName.MiddleName,
                    enrollment.EnrolledAtUtc,
                    enrollment.WithdrawnAtUtc
                })
            .ToListAsync(cancellationToken);

        return new StudyGroupDetails(
            row.Id, row.Name, row.CourseId, row.CourseName, row.Category, row.TheoryInstructorId, InstructorName(row),
            row.BranchId, row.BranchName, row.StartDate, row.EndDate, row.Capacity, row.IsActive,
            [
                .. members.Select(member => new StudyGroupMemberItem(
                    member.Id, member.ContractId, member.Number, member.StudentId,
                    PersonNames.Format(member.LastName, member.FirstName, member.MiddleName)!, member.EnrolledAtUtc,
                    member.WithdrawnAtUtc))
            ]);
    }

    private static string InstructorName(GroupRow row) =>
        PersonNames.Format(row.InstructorLastName, row.InstructorFirstName, row.InstructorMiddleName)!;

    private IQueryable<GroupRow> Rows() =>
        from studyGroup in dbContext.Set<StudyGroup>().AsNoTracking()
        join course in dbContext.Set<Course>() on studyGroup.CourseId equals course.Id
        join instructor in dbContext.Set<User>() on studyGroup.TheoryInstructorId equals instructor.Id
        join branch in dbContext.Set<Branch>() on studyGroup.BranchId equals (Guid?)branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        select new GroupRow
        {
            Id = studyGroup.Id,
            Name = studyGroup.Name,
            CourseId = studyGroup.CourseId,
            CourseName = course.Name,
            Category = studyGroup.Category,
            TheoryInstructorId = studyGroup.TheoryInstructorId,
            InstructorLastName = instructor.FullName.LastName,
            InstructorFirstName = instructor.FullName.FirstName,
            InstructorMiddleName = instructor.FullName.MiddleName,
            BranchId = studyGroup.BranchId,
            BranchName = branch == null ? null : branch.Name,
            StartDate = studyGroup.StartDate,
            EndDate = studyGroup.EndDate,
            Capacity = studyGroup.Capacity,
            CurrentMemberCount = studyGroup.Enrollments.Count(enrollment => enrollment.WithdrawnAtUtc == null),
            IsActive = studyGroup.IsActive
        };

    /// <summary>The columns of a group that the read models are built from.</summary>
    private sealed class GroupRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = null!;

        public Guid CourseId { get; init; }

        public string CourseName { get; init; } = null!;

        public LicenseCategory Category { get; init; }

        public Guid TheoryInstructorId { get; init; }

        public string InstructorLastName { get; init; } = null!;

        public string InstructorFirstName { get; init; } = null!;

        public string? InstructorMiddleName { get; init; }

        public Guid? BranchId { get; init; }

        public string? BranchName { get; init; }

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }

        public int Capacity { get; init; }

        public int CurrentMemberCount { get; init; }

        public bool IsActive { get; init; }
    }
}

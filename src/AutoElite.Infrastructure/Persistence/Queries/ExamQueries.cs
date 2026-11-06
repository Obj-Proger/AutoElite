using System.Linq.Expressions;
using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Exams;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads exams with their group, template and results, without tracking.</summary>
internal sealed class ExamQueries(AutoEliteDbContext dbContext) : IExamQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ExamItem>> GetScheduleAsync(
        ExamScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var period = filter.Period;
        var rows = Rows().Where(row => period.StartUtc <= row.ScheduledAtUtc && row.ScheduledAtUtc < period.EndUtc);
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (filter.Kind is { } kind)
            rows = rows.Where(row => row.Kind == kind);

        if (filter.GroupId is { } groupId)
            rows = rows.Where(row => row.GroupId == groupId);

        if (filter.ResponsibleInstructorId is { } instructorId)
            rows = rows.Where(row => row.ResponsibleInstructorId == instructorId);

        var found = await Earliest(rows).ToListAsync(cancellationToken);

        return [.. found.Select(ToItem)];
    }

    /// <inheritdoc />
    public async Task<GroupExams?> GetOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.Set<StudyGroup>().AsNoTracking()
            .Where(found => found.Id == groupId)
            .Select(found => new { found.BranchId, found.TheoryInstructorId })
            .FirstOrDefaultAsync(cancellationToken);
        if (group is null)
            return null;

        var exams = await Earliest(Rows().Where(row => row.GroupId == groupId)).ToListAsync(cancellationToken);

        return new GroupExams(group.BranchId, group.TheoryInstructorId, [.. exams.Select(ToItem)]);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudentExamItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = dbContext.Set<GroupEnrollment>();
        var results = dbContext.Set<ExamResult>();
        var exams = await Rows()
            .Where(row =>
                enrollments.Any(enrollment =>
                    EF.Property<Guid>(enrollment, StudyGroupConfiguration.GroupKey) == row.GroupId
                    && enrollment.StudentId == studentId
                    && enrollment.WithdrawnAtUtc == null)
                || results.Any(result =>
                    EF.Property<Guid>(result, ExamConfiguration.ExamKey) == row.Id && result.StudentId == studentId))
            .OrderByDescending(row => row.ScheduledAtUtc)
            .ThenByDescending(row => row.Id)
            .ToListAsync(cancellationToken);
        List<Guid> examIds = [.. exams.Select(exam => exam.Id)];
        var own = await Results(result =>
                examIds.Contains(EF.Property<Guid>(result, ExamConfiguration.ExamKey)) && result.StudentId == studentId)
            .ToListAsync(cancellationToken);
        var ownByExam = own.ToLookup(result => result.ExamId);

        return
        [
            .. exams.Select(exam => new StudentExamItem(ToItem(exam), ownByExam[exam.Id].Select(ToResultItem).FirstOrDefault()))
        ];
    }

    /// <inheritdoc />
    public async Task<ExamDetails?> GetDetailsAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == examId, cancellationToken);
        if (row is null)
            return null;

        var results = await Results(result => EF.Property<Guid>(result, ExamConfiguration.ExamKey) == examId)
            .ToListAsync(cancellationToken);

        return new ExamDetails(
            ToItem(row),
            [
                .. results
                    .Select(ToResultItem)
                    .OrderBy(result => result.StudentName, StringComparer.CurrentCulture)
                    .ThenBy(result => result.ContractNumber, StringComparer.Ordinal)
            ]);
    }

    private static IQueryable<ExamRow> Earliest(IQueryable<ExamRow> rows) =>
        rows.OrderBy(row => row.ScheduledAtUtc).ThenBy(row => row.Id);

    private static ExamItem ToItem(ExamRow row) =>
        new(
            row.Id, row.GroupId, row.GroupName, row.BranchId, row.ResponsibleInstructorId, row.Kind, row.TestTemplateId,
            row.TestTemplateName, row.ScheduledAtUtc, row.Status, row.CancellationReason, row.ResultCount);

    private static ExamResultItem ToResultItem(ResultRow row) =>
        new(
            row.ContractId, row.ContractNumber, row.StudentId,
            PersonNames.Format(row.StudentLastName, row.StudentFirstName, row.StudentMiddleName)!, row.Passed, row.Score,
            row.Comment, row.TestSessionId, row.RecordedByUserId, row.RecordedAtUtc, row.CorrectedByUserId,
            row.CorrectedAtUtc);

    private IQueryable<ExamRow> Rows() =>
        from exam in dbContext.Set<Exam>().AsNoTracking()
        join studyGroup in dbContext.Set<StudyGroup>() on exam.GroupId equals studyGroup.Id
        join template in dbContext.Set<TestTemplate>() on exam.TestTemplateId equals (Guid?)template.Id into templates
        from template in templates.DefaultIfEmpty()
        select new ExamRow
        {
            Id = exam.Id,
            GroupId = exam.GroupId,
            GroupName = studyGroup.Name,
            BranchId = studyGroup.BranchId,
            ResponsibleInstructorId = studyGroup.TheoryInstructorId,
            Kind = exam.Kind,
            TestTemplateId = exam.TestTemplateId,
            TestTemplateName = template == null ? null : template.Name,
            ScheduledAtUtc = exam.ScheduledAtUtc,
            Status = exam.Status,
            CancellationReason = exam.CancellationReason,
            ResultCount = exam.Results.Count
        };

    /// <summary>The results that match the condition, with the contract number and the student's name.</summary>
    private IQueryable<ResultRow> Results(Expression<Func<ExamResult, bool>> condition) =>
        from result in dbContext.Set<ExamResult>().AsNoTracking().Where(condition)
        join contract in dbContext.Set<Contract>() on result.ContractId equals contract.Id
        join student in dbContext.Set<User>() on result.StudentId equals student.Id
        select new ResultRow
        {
            ExamId = EF.Property<Guid>(result, ExamConfiguration.ExamKey),
            ContractId = result.ContractId,
            ContractNumber = contract.Number,
            StudentId = result.StudentId,
            StudentLastName = student.FullName.LastName,
            StudentFirstName = student.FullName.FirstName,
            StudentMiddleName = student.FullName.MiddleName,
            Passed = result.Passed,
            Score = result.Score,
            Comment = result.Comment,
            TestSessionId = result.TestSessionId,
            RecordedByUserId = result.RecordedByUserId,
            RecordedAtUtc = result.RecordedAtUtc,
            CorrectedByUserId = result.CorrectedByUserId,
            CorrectedAtUtc = result.CorrectedAtUtc
        };

    /// <summary>The columns of an exam that the list item is built from.</summary>
    private sealed class ExamRow
    {
        public Guid Id { get; init; }

        public Guid GroupId { get; init; }

        public string GroupName { get; init; } = null!;

        public Guid? BranchId { get; init; }

        public Guid ResponsibleInstructorId { get; init; }

        public ExamKind Kind { get; init; }

        public Guid? TestTemplateId { get; init; }

        public string? TestTemplateName { get; init; }

        public DateTime ScheduledAtUtc { get; init; }

        public ExamStatus Status { get; init; }

        public string? CancellationReason { get; init; }

        public int ResultCount { get; init; }
    }

    /// <summary>The columns of a result with its contract and student.</summary>
    private sealed class ResultRow
    {
        public Guid ExamId { get; init; }

        public Guid ContractId { get; init; }

        public string ContractNumber { get; init; } = null!;

        public Guid StudentId { get; init; }

        public string StudentLastName { get; init; } = null!;

        public string StudentFirstName { get; init; } = null!;

        public string? StudentMiddleName { get; init; }

        public bool Passed { get; init; }

        public int? Score { get; init; }

        public string? Comment { get; init; }

        public Guid? TestSessionId { get; init; }

        public Guid? RecordedByUserId { get; init; }

        public DateTime RecordedAtUtc { get; init; }

        public Guid? CorrectedByUserId { get; init; }

        public DateTime? CorrectedAtUtc { get; init; }
    }
}

using AutoElite.Application.Contracts;
using AutoElite.Application.Exams;
using AutoElite.Application.StudentCards;
using AutoElite.Application.TestSessions;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Builds the student card from the read models of the modules it summarizes, so that the card shows a contract, an
/// attempt or an exam exactly as their own pages do; the counts of lessons are read here.
/// </summary>
/// <remarks>
/// A lesson of the current group counts as held when it is completed, or scheduled and already over; only lessons
/// that began after the contract was enrolled in the group count, since the student could not attend earlier ones.
/// </remarks>
internal sealed class StudentCardQueries(
    AutoEliteDbContext dbContext,
    IContractQueries contractQueries,
    ITestSessionQueries testSessionQueries,
    IExamQueries examQueries) : IStudentCardQueries
{
    /// <inheritdoc />
    public async Task<StudentCard?> GetAsync(Guid studentId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var student = await dbContext.Set<User>().AsNoTracking()
            .Where(user => user.Id == studentId)
            .Select(user => new
            {
                user.FullName.LastName,
                user.FullName.FirstName,
                user.FullName.MiddleName,
                user.Email,
                user.Phone,
                user.PhotoDocumentId,
                user.BranchId,
                user.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (student is null)
            return null;

        List<Guid> contractIds = await dbContext.Set<Contract>().AsNoTracking()
            .Where(contract => contract.StudentId == studentId)
            .OrderByDescending(contract => contract.CreatedAtUtc)
            .ThenByDescending(contract => contract.Id)
            .Select(contract => contract.Id)
            .ToListAsync(cancellationToken);
        var enrollments = await EnrollmentsAsync(contractIds, cancellationToken);
        var driving = await DrivingAsync(contractIds, utcNow, cancellationToken);
        IReadOnlyList<StudentExamItem> exams =
            contractIds.Count == 0 ? [] : await examQueries.GetOfStudentAsync(studentId, cancellationToken);

        var contracts = new List<StudentCardContract>(contractIds.Count);
        foreach (var contractId in contractIds)
        {
            var details = await contractQueries.GetDetailsAsync(contractId, cancellationToken);
            if (details is null)
                continue;

            var ofContract = enrollments.Where(enrollment => enrollment.ContractId == contractId).ToList();
            var groupIds = ofContract.Select(enrollment => enrollment.GroupId).ToHashSet();
            var current = ofContract.FirstOrDefault(enrollment => enrollment.WithdrawnAtUtc is null);

            // An exam of the contract's groups, with the result only if it is this contract's.
            StudentExamItem[] contractExams =
            [
                .. exams
                    .Where(exam => groupIds.Contains(exam.Exam.GroupId) || exam.Result?.ContractId == contractId)
                    .Select(exam => exam.Result is null || exam.Result.ContractId == contractId ? exam : exam with { Result = null })
            ];
            contracts.Add(new StudentCardContract(
                details,
                await GroupAsync(contractId, current, utcNow, cancellationToken),
                driving.GetValueOrDefault(contractId) ?? new StudentCardDriving(0, 0, 0, null),
                await TestsAsync(contractId, cancellationToken),
                contractExams));
        }

        return new StudentCard(
            studentId, student.LastName, student.FirstName, student.MiddleName, student.Email.Value, student.Phone?.Value,
            student.PhotoDocumentId, student.BranchId, student.IsActive, contracts);
    }

    /// <summary>Hides the counts of an attempt in progress, as the attempt's own page does (FR-7.7).</summary>
    private static TestSessionItem Shown(TestSessionItem attempt) =>
        attempt.Status == TestSessionStatus.InProgress ? attempt with { CorrectAnswerCount = null, MistakeCount = null } : attempt;

    private async Task<List<EnrollmentRow>> EnrollmentsAsync(List<Guid> contractIds, CancellationToken cancellationToken) =>
        contractIds.Count == 0
            ? []
            : await (
                    from enrollment in dbContext.Set<GroupEnrollment>().AsNoTracking()
                    join studyGroup in dbContext.Set<StudyGroup>()
                        on EF.Property<Guid>(enrollment, StudyGroupConfiguration.GroupKey) equals studyGroup.Id
                    where contractIds.Contains(enrollment.ContractId)
                    orderby enrollment.EnrolledAtUtc descending
                    select new EnrollmentRow(
                        enrollment.ContractId, studyGroup.Id, studyGroup.Name, enrollment.EnrolledAtUtc, enrollment.WithdrawnAtUtc))
                .ToListAsync(cancellationToken);

    /// <summary>The current group of a contract with the lessons held since the enrollment and those attended.</summary>
    private async Task<StudentCardGroup?> GroupAsync(
        Guid contractId, EnrollmentRow? current, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (current is null)
            return null;

        var (groupId, enrolledAtUtc) = (current.GroupId, current.EnrolledAtUtc);
        var held = await dbContext.Set<TheoryLesson>().AsNoTracking()
            .Where(lesson => lesson.GroupId == groupId
                && lesson.Time.StartUtc >= enrolledAtUtc
                && (lesson.Status == TheoryLessonStatus.Completed
                    || (lesson.Status == TheoryLessonStatus.Scheduled && lesson.Time.EndUtc <= utcNow)))
            .Select(lesson => lesson.Attendance.Any(attendance => attendance.ContractId == contractId && attendance.IsPresent))
            .ToListAsync(cancellationToken);

        return new StudentCardGroup(groupId, current.GroupName, held.Count, held.Count(attended => attended));
    }

    /// <summary>The driving lessons of the contracts, by contract.</summary>
    private async Task<Dictionary<Guid, StudentCardDriving>> DrivingAsync(
        List<Guid> contractIds, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
            return new Dictionary<Guid, StudentCardDriving>();

        var bookings = await (
                from slot in dbContext.Set<DrivingSlot>().AsNoTracking()
                from booking in slot.Bookings
                where contractIds.Contains(booking.ContractId)
                select new { booking.ContractId, booking.Status, booking.StudentRating, slot.Time.StartUtc })
            .ToListAsync(cancellationToken);

        return bookings
            .GroupBy(row => row.ContractId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var ratings = group.Where(row => row.StudentRating is not null).ToList();

                    return new StudentCardDriving(
                        group.Count(row => row.Status == DrivingBookingStatus.Completed),
                        group.Count(row => row.Status == DrivingBookingStatus.NoShow),
                        group.Count(row => row.Status == DrivingBookingStatus.Active && row.StartUtc > utcNow),
                        ratings.Count == 0 ? null : Math.Round((decimal)ratings.Average(row => row.StudentRating!.Value), 2));
                });
    }

    /// <summary>The practice attempts of a contract; attempts of exams are shown with the exams.</summary>
    private async Task<StudentCardTests> TestsAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var sessions = await testSessionQueries.GetOfContractAsync(contractId, cancellationToken);
        var practice = sessions?.Sessions.Where(session => session.ExamId is null).ToList() ?? [];

        // The attempts come the latest first.
        return new StudentCardTests(
            practice.Count,
            practice.Count(session => session.Status == TestSessionStatus.Passed),
            practice.Count(session => session.Status == TestSessionStatus.Failed),
            practice.Count == 0 ? null : Shown(practice[0]));
    }

    /// <summary>An enrollment of one of the student's contracts, the latest first.</summary>
    private sealed record EnrollmentRow(
        Guid ContractId, Guid GroupId, string GroupName, DateTime EnrolledAtUtc, DateTime? WithdrawnAtUtc);
}

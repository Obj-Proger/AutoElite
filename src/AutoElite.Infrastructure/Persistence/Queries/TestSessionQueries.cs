using AutoElite.Application.TestSessions;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads test attempts with their template names and results, without tracking.</summary>
internal sealed class TestSessionQueries(AutoEliteDbContext dbContext) : ITestSessionQueries
{
    /// <inheritdoc />
    public async Task<ContractTestSessions?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Set<Contract>().AsNoTracking()
            .Where(found => found.Id == contractId)
            .Select(found => new { found.StudentId, found.BranchId })
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null)
            return null;

        var sessions = await Items(dbContext.Set<TestSession>().AsNoTracking().Where(session => session.ContractId == contractId))
            .OrderByDescending(item => item.StartedAtUtc)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        return new ContractTestSessions(contract.StudentId, contract.BranchId, [.. sessions.Select(ToItem)]);
    }

    /// <inheritdoc />
    public async Task<TestSessionDetails?> GetDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var item = await Items(dbContext.Set<TestSession>().AsNoTracking().Where(session => session.Id == sessionId))
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
            return null;

        var session = await dbContext.Set<TestSession>().AsNoTracking()
            .Include(found => found.Questions.OrderBy(question => question.Position))
            .FirstAsync(found => found.Id == sessionId, cancellationToken);
        var branchId = await dbContext.Set<Contract>().AsNoTracking()
            .Where(contract => contract.Id == session.ContractId)
            .Select(contract => contract.BranchId)
            .FirstOrDefaultAsync(cancellationToken);

        return new TestSessionDetails(
            ToItem(item), session.ContractId, session.StudentId, branchId,
            [
                .. session.Questions.Select(question => new SessionQuestionItem(
                    question.Id, question.Position, question.BlockNumber, question.IsAddOn, question.Text,
                    question.ImageDocumentId,
                    [.. question.Options.Select(option => new SessionOptionItem(option.OptionId, option.Text))],
                    question.SelectedOptionId, question.CorrectOptionId, question.IsCorrect, question.Explanation))
            ]);
    }

    private static TestSessionItem ToItem(SessionRow row) =>
        new(
            row.Id, row.TemplateId, row.TemplateName, row.ExamId, row.Category, row.Status, row.FailureReason,
            row.StartedAtUtc, row.DeadlineUtc, row.FinishedAtUtc, row.QuestionCount, row.CorrectAnswerCount,
            row.MistakeCount);

    /// <summary>The attempts with their template name and counts computed by SQL.</summary>
    private IQueryable<SessionRow> Items(IQueryable<TestSession> sessions) =>
        from session in sessions
        join template in dbContext.Set<TestTemplate>() on session.TemplateId equals template.Id
        select new SessionRow
        {
            Id = session.Id,
            TemplateId = session.TemplateId,
            TemplateName = template.Name,
            ExamId = session.ExamId,
            Category = session.Category,
            Status = session.Status,
            FailureReason = session.FailureReason,
            StartedAtUtc = session.StartedAtUtc,
            DeadlineUtc = session.DeadlineUtc,
            FinishedAtUtc = session.FinishedAtUtc,
            QuestionCount = session.Questions.Count,
            CorrectAnswerCount = session.Questions.Count(question =>
                question.SelectedOptionId != null && question.SelectedOptionId == question.CorrectOptionId),
            MistakeCount = session.Questions.Count(question =>
                !question.IsAddOn && question.SelectedOptionId != null && question.SelectedOptionId != question.CorrectOptionId)
        };

    /// <summary>The columns of an attempt that the list item is built from.</summary>
    private sealed class SessionRow
    {
        public Guid Id { get; init; }

        public Guid TemplateId { get; init; }

        public string TemplateName { get; init; } = null!;

        public Guid? ExamId { get; init; }

        public LicenseCategory Category { get; init; }

        public TestSessionStatus Status { get; init; }

        public TestFailureReason? FailureReason { get; init; }

        public DateTime StartedAtUtc { get; init; }

        public DateTime DeadlineUtc { get; init; }

        public DateTime? FinishedAtUtc { get; init; }

        public int QuestionCount { get; init; }

        public int CorrectAnswerCount { get; init; }

        public int MistakeCount { get; init; }
    }
}

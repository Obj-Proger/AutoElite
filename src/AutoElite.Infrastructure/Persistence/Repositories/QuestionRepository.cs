using AutoElite.Application.Questions;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads questions with their answer options in order; external codes are matched regardless of case.</summary>
internal sealed class QuestionRepository(AutoEliteDbContext dbContext) : Repository<Question>(dbContext), IQuestionRepository
{
    /// <inheritdoc />
    public override Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithOptions().FirstOrDefaultAsync(question => question.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExternalCodeExistsAsync(
        string externalCode, Guid? exceptQuestionId = null, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(
            question => question.ExternalCode == externalCode && question.Id != exceptQuestionId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Question>> GetByExternalCodesAsync(
        IReadOnlyCollection<string> externalCodes, CancellationToken cancellationToken = default) =>
        externalCodes.Count == 0
            ? []
            : await WithOptions()
                .Where(question => question.ExternalCode != null && externalCodes.Contains(question.ExternalCode))
                .ToListAsync(cancellationToken);

    private IQueryable<Question> WithOptions() =>
        Aggregates.Include(question => question.Options.OrderBy(option => option.Position));
}

/// <summary>Loads and adds test templates; their rules and blocks are columns of the template.</summary>
internal sealed class TestTemplateRepository(AutoEliteDbContext dbContext) : Repository<TestTemplate>(dbContext);

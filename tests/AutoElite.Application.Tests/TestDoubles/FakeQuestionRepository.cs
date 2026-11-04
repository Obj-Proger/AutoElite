using AutoElite.Application.Questions;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeQuestionRepository : InMemoryRepository<Question>, IQuestionRepository
{
    public Task<bool> ExternalCodeExistsAsync(
        string externalCode, Guid? exceptQuestionId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(question =>
            question.Id != exceptQuestionId && string.Equals(question.ExternalCode, externalCode, StringComparison.OrdinalIgnoreCase)));
}

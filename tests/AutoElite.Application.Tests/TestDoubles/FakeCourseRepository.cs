using AutoElite.Application.Courses;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeCourseRepository : InMemoryRepository<Course>, ICourseRepository
{
    public HashSet<Guid> TopicsInUse { get; } = [];

    public Task<bool> IsTopicInUseAsync(Guid topicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(TopicsInUse.Contains(topicId));
}

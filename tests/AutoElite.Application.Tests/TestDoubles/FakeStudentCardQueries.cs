using AutoElite.Application.StudentCards;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the time asked with; the reading itself is the infrastructure's job.</summary>
internal sealed class FakeStudentCardQueries : IStudentCardQueries
{
    private readonly Dictionary<Guid, StudentCard> _cards = [];

    public DateTime? LastUtcNow { get; private set; }

    public void Seed(StudentCard card) => _cards.Add(card.StudentId, card);

    public Task<StudentCard?> GetAsync(Guid studentId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        LastUtcNow = utcNow;

        return Task.FromResult(_cards.GetValueOrDefault(studentId));
    }
}

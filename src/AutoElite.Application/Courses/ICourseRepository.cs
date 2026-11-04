namespace AutoElite.Application.Courses;

/// <summary>Loads and adds courses for commands; a loaded course includes its topics.</summary>
public interface ICourseRepository : IRepository<Course>
{
    /// <summary>
    /// Determines whether anything refers to the topic: a question of the bank, a theory lesson or a block of a test
    /// template. Such a topic cannot be removed from the curriculum.
    /// </summary>
    /// <param name="topicId">The identifier of the topic.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> IsTopicInUseAsync(Guid topicId, CancellationToken cancellationToken = default);
}

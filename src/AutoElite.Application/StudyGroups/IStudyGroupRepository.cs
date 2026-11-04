namespace AutoElite.Application.StudyGroups;

/// <summary>Loads and adds study groups for commands; a loaded group includes its enrollments.</summary>
public interface IStudyGroupRepository : IRepository<StudyGroup>
{
    /// <summary>Loads the groups in which the contract is a current member.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StudyGroup>> GetWithCurrentMemberAsync(Guid contractId, CancellationToken cancellationToken = default);
}

namespace AutoElite.Application.Questions;

/// <summary>Loads and adds questions for commands; a loaded question includes its answer options.</summary>
public interface IQuestionRepository : IRepository<Question>
{
    /// <summary>Determines whether another question, active or not, already has the external code, ignoring case.</summary>
    /// <param name="externalCode">The trimmed code.</param>
    /// <param name="exceptQuestionId">A question to leave out, when the code of that question is being changed.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> ExternalCodeExistsAsync(
        string externalCode, Guid? exceptQuestionId = null, CancellationToken cancellationToken = default);
}

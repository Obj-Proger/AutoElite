namespace AutoElite.Application.StaffTasks;

/// <summary>Turns the record a task is about, given as a kind and an identifier, into a checked reference (FR-12.1).</summary>
internal sealed class TaskSubjects(IEntityReferences entityReferences)
{
    /// <summary>Builds the reference and checks that the record exists.</summary>
    /// <param name="kind">The kind of the record, or <see langword="null"/> if the task is about nothing in particular.</param>
    /// <param name="id">The identifier of the record; given together with the kind.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The reference, <see langword="null"/> for no record, or a failure.</returns>
    public async Task<Result<EntityReference?>> ResolveAsync(EntityKind? kind, Guid? id, CancellationToken cancellationToken)
    {
        if (kind is null && id is null)
            return Result.Success<EntityReference?>(null);

        if (kind is not { } subjectKind || id is not { } subjectId)
            return Result.Failure<EntityReference?>(ApplicationErrors.StaffTask.SubjectIncomplete);

        var referenceResult = EntityReference.Create(subjectKind, subjectId);
        if (referenceResult.IsFailure)
            return Result.Failure<EntityReference?>(referenceResult.Error);

        return await entityReferences.ExistsAsync(referenceResult.Value, cancellationToken)
            ? Result.Success<EntityReference?>(referenceResult.Value)
            : Result.Failure<EntityReference?>(ApplicationErrors.StaffTask.SubjectNotFound);
    }
}

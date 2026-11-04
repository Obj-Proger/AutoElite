namespace AutoElite.Application.Leads.AddLeadNote;

/// <summary>Adds the note with the caller as the author and returns its identifier.</summary>
internal sealed class AddLeadNoteCommandHandler(LeadAccess leadAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<AddLeadNoteCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddLeadNoteCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure<Guid>(leadResult.Error);

        var noteResult = leadResult.Value.AddNote(
            command.Text, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);

        return noteResult.IsFailure ? Result.Failure<Guid>(noteResult.Error) : noteResult.Value.Id;
    }
}

namespace AutoElite.Application.Leads.UpdateLeadContact;

/// <summary>Applies the contact details to the lead.</summary>
internal sealed class UpdateLeadContactCommandHandler(LeadAccess leadAccess) : ICommandHandler<UpdateLeadContactCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateLeadContactCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        var contactResult = LeadContact.Parse(command.Phone, command.Email);
        if (contactResult.IsFailure)
            return Result.Failure(contactResult.Error);

        return leadResult.Value.UpdateContact(
            command.ContactName, contactResult.Value.Phone, contactResult.Value.Email, command.DesiredCategory, command.Comment);
    }
}

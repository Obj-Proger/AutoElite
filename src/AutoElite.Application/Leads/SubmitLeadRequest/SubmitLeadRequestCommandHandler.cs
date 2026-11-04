namespace AutoElite.Application.Leads.SubmitLeadRequest;

/// <summary>Creates a website lead after checking the chosen branch.</summary>
internal sealed class SubmitLeadRequestCommandHandler(
    IRepository<Lead> leadRepository,
    IRepository<Branch> branchRepository,
    TimeProvider timeProvider) : ICommandHandler<SubmitLeadRequestCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SubmitLeadRequestCommand command, CancellationToken cancellationToken)
    {
        var branchResult = await BranchChecks.CheckAsync(branchRepository, command.BranchId, DataScope.SchoolWide, cancellationToken);
        if (branchResult.IsFailure)
            return branchResult;

        var contactResult = LeadContact.Parse(command.Phone, command.Email);
        if (contactResult.IsFailure)
            return Result.Failure(contactResult.Error);

        var leadResult = Lead.Create(
            command.ContactName,
            contactResult.Value.Phone,
            contactResult.Value.Email,
            LeadSource.Website,
            command.DesiredCategory,
            command.Comment,
            command.BranchId,
            null,
            timeProvider.GetUtcNow().UtcDateTime);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        leadRepository.Add(leadResult.Value);

        return Result.Success();
    }
}

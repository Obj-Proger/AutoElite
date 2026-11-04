namespace AutoElite.Application.Leads.CreateLead;

/// <summary>Creates the lead in the caller's scope and assigns the responsible manager, if any.</summary>
internal sealed class CreateLeadCommandHandler(
    IRepository<Lead> leadRepository,
    IRepository<Branch> branchRepository,
    LeadManagers leadManagers,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<CreateLeadCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateLeadCommand command, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);
        var branchId = command.BranchId ?? scope.BranchId;
        var branchResult = await BranchChecks.CheckAsync(branchRepository, branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var managerId = command.ManagerId ?? (currentUser.IsInRole(UserRole.Manager) ? currentUser.UserId : null);
        if (managerId is { } id)
        {
            var managerResult = await leadManagers.CheckAsync(id, branchId, cancellationToken);
            if (managerResult.IsFailure)
                return Result.Failure<Guid>(managerResult.Error);
        }

        var contactResult = LeadContact.Parse(command.Phone, command.Email);
        if (contactResult.IsFailure)
            return Result.Failure<Guid>(contactResult.Error);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var leadResult = Lead.Create(
            command.ContactName,
            contactResult.Value.Phone,
            contactResult.Value.Email,
            command.Source,
            command.DesiredCategory,
            command.Comment,
            branchId,
            currentUser.GetRequiredUserId(),
            utcNow);
        if (leadResult.IsFailure)
            return Result.Failure<Guid>(leadResult.Error);

        var lead = leadResult.Value;
        if (managerId is { } assignedId)
        {
            var assignResult = lead.AssignManager(assignedId, utcNow);
            if (assignResult.IsFailure)
                return Result.Failure<Guid>(assignResult.Error);
        }

        leadRepository.Add(lead);

        return lead.Id;
    }
}

using AutoElite.Application.Common.Events;

namespace AutoElite.Application.StudyGroups.ContractClosure;

/// <summary>
/// Withdraws the student of a completed or terminated contract from every group, so that closed contracts do not
/// take places in groups. Withdrawing is allowed in deactivated groups too.
/// </summary>
internal sealed class WithdrawFromGroupsWhenContractClosedHandler(IStudyGroupRepository groupRepository)
    : IDomainEventHandler<ContractClosedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(ContractClosedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var groups = await groupRepository.GetWithCurrentMemberAsync(domainEvent.ContractId, cancellationToken);

        // Withdrawing at the time of closing keeps the history right even if the event is delivered late.
        foreach (var group in groups)
            group.Withdraw(domainEvent.ContractId, domainEvent.OccurredOnUtc);
    }
}

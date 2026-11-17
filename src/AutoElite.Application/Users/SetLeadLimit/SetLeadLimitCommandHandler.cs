namespace AutoElite.Application.Users.SetLeadLimit;

/// <summary>Sets the limit of leads in progress of the user.</summary>
internal sealed class SetLeadLimitCommandHandler(ManagedUsers managedUsers) : ICommandHandler<SetLeadLimitCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SetLeadLimitCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);

        return managedResult.IsFailure
            ? Result.Failure(managedResult.Error)
            : managedResult.Value.User.SetLeadLimit(command.LeadLimit);
    }
}

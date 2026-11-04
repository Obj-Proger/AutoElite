namespace AutoElite.Application.Users.UpdateUserDetails;

/// <summary>Applies the new name and phone to the user.</summary>
internal sealed class UpdateUserDetailsCommandHandler(ManagedUsers managedUsers) : ICommandHandler<UpdateUserDetailsCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUserDetailsCommand command, CancellationToken cancellationToken)
    {
        var managedResult = await managedUsers.LoadAsync(command.UserId, cancellationToken);
        if (managedResult.IsFailure)
            return Result.Failure(managedResult.Error);

        var fullNameResult = FullName.Create(command.LastName, command.FirstName, command.MiddleName);
        if (fullNameResult.IsFailure)
            return Result.Failure(fullNameResult.Error);

        var phoneResult = ContactDetails.CreateOptionalPhone(command.Phone);
        if (phoneResult.IsFailure)
            return Result.Failure(phoneResult.Error);

        return managedResult.Value.User.UpdateProfile(fullNameResult.Value, phoneResult.Value);
    }
}

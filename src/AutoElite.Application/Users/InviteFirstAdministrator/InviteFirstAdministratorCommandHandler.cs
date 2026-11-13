namespace AutoElite.Application.Users.InviteFirstAdministrator;

/// <summary>Registers the administrator while the school has none and gives the user, or nothing when it has one.</summary>
internal sealed class InviteFirstAdministratorCommandHandler(
    UserRegistration userRegistration,
    IUserRoleRepository userRoleRepository) : ICommandHandler<InviteFirstAdministratorCommand, Guid?>
{
    private static readonly UserRole[] _administrator = [UserRole.Administrator];

    public async Task<Result<Guid?>> Handle(InviteFirstAdministratorCommand command, CancellationToken cancellationToken)
    {
        // Two hosts that start at once cannot both register the user, since the address is unique among all users.
        if (await userRoleRepository.CountActiveAdministratorsAsync(cancellationToken) > 0)
            return Result.Success<Guid?>(null);

        var details = new NewUserDetails(
            command.LastName, command.FirstName, null, command.Email, null, null, command.PreferredLanguage);
        var userResult = await userRegistration.RegisterAsync(details, _administrator, cancellationToken);

        return userResult.IsFailure ? Result.Failure<Guid?>(userResult.Error) : userResult.Value.Id;
    }
}

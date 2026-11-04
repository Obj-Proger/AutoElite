using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.RequestEmailChange;

/// <summary>Checks the password and the new address, then emails the confirmation link to the new address.</summary>
internal sealed class RequestEmailChangeCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    IClientLinks clientLinks,
    IEmailQueue emailQueue,
    ICurrentUser currentUser) : ICommandHandler<RequestEmailChangeCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RequestEmailChangeCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);
        if (user is null)
            return Result.Failure(ApplicationErrors.NotFound<User>());

        if (!user.IsActive)
            return Result.Failure(ApplicationErrors.Account.UserDeactivated);

        var emailResult = Email.Create(command.NewEmail);
        if (emailResult.IsFailure)
            return Result.Failure(emailResult.Error);

        var newEmail = emailResult.Value;
        if (newEmail == user.Email)
            return Result.Failure(DomainErrors.User.EmailUnchanged);

        var check = await identityService.CheckPasswordAsync(user.Id, command.CurrentPassword!, cancellationToken);
        if (check == PasswordCheckResult.LockedOut)
            return Result.Failure(ApplicationErrors.Account.LockedOut);

        if (check == PasswordCheckResult.WrongPassword)
            return Result.Failure(ApplicationErrors.Account.CurrentPasswordWrong);

        if (await userRepository.EmailExistsAsync(newEmail, cancellationToken))
            return Result.Failure(ApplicationErrors.Account.EmailTaken);

        var token = await identityService.GenerateEmailChangeTokenAsync(user.Id, newEmail, cancellationToken);
        var link = clientLinks.EmailChange(user.Id, newEmail, token);
        var message = UserEmail.Compose(user, newEmail, EmailTexts.EmailChangeSubject, EmailTexts.EmailChangeBody, link);

        await emailQueue.EnqueueAsync(message, cancellationToken);

        return Result.Success();
    }
}

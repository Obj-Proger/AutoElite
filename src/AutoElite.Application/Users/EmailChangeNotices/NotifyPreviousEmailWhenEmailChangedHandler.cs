using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Users.EmailChangeNotices;

/// <summary>
/// Tells the previous address that the account now uses another one, so that the owner notices a change made by
/// someone else.
/// </summary>
internal sealed class NotifyPreviousEmailWhenEmailChangedHandler(IUserRepository userRepository, IEmailQueue emailQueue)
    : IDomainEventHandler<UserEmailChangedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(UserEmailChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
        var previousEmail = Email.Create(domainEvent.PreviousEmail);
        if (user is null || previousEmail.IsFailure)
            return;

        var message = UserEmail.Compose(
            user, previousEmail.Value, EmailTexts.EmailChangedSubject, EmailTexts.EmailChangedBody, domainEvent.NewEmail);

        await emailQueue.EnqueueAsync(message, cancellationToken);
    }
}

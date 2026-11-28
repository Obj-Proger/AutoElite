namespace AutoElite.Application.Notifications.RegisterPushDevice;

/// <summary>Passes a known token to the signed-in user, or registers a new device for them.</summary>
internal sealed class RegisterPushDeviceCommandHandler(
    IPushDeviceRepository deviceRepository, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<RegisterPushDeviceCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RegisterPushDeviceCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var token = command.Token?.Trim() ?? string.Empty;
        var device = token.Length == 0 ? null : await deviceRepository.FindByTokenAsync(token, cancellationToken);
        if (device is not null)
            return device.Reassign(userId, utcNow);

        var registered = PushDevice.Register(userId, token, command.Platform, utcNow);
        if (registered.IsFailure)
            return Result.Failure(registered.Error);

        deviceRepository.Add(registered.Value);

        return Result.Success();
    }
}

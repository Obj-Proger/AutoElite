namespace AutoElite.Application.Notifications.UnregisterPushDevice;

/// <summary>Removes the device when it notifies the signed-in user.</summary>
internal sealed class UnregisterPushDeviceCommandHandler(IPushDeviceRepository deviceRepository, ICurrentUser currentUser)
    : ICommandHandler<UnregisterPushDeviceCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UnregisterPushDeviceCommand command, CancellationToken cancellationToken)
    {
        var token = command.Token?.Trim() ?? string.Empty;
        if (token.Length == 0)
            return Result.Success();

        var device = await deviceRepository.FindByTokenAsync(token, cancellationToken);
        if (device is not null && device.UserId == currentUser.GetRequiredUserId())
            deviceRepository.Remove(device);

        return Result.Success();
    }
}

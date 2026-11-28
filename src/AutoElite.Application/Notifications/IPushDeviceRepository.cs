namespace AutoElite.Application.Notifications;

/// <summary>Loads, adds and removes the devices that receive push notifications.</summary>
public interface IPushDeviceRepository : IRepository<PushDevice>
{
    /// <summary>Loads the device with a token, tracked, whoever it notifies.</summary>
    /// <param name="token">The token of the device in the push service.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The device, or <see langword="null"/> if no device has the token.</returns>
    Task<PushDevice?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Loads the devices of a user, tracked.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The devices, possibly none.</returns>
    Task<IReadOnlyList<PushDevice>> ListOfUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Marks a device for removal when the changes are saved.</summary>
    /// <param name="device">The device.</param>
    void Remove(PushDevice device);
}

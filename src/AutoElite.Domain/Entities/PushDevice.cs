using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A device of a user that receives push notifications: the token the push service gave the application on it. The
/// token belongs to the installation, not to the user, so when another user signs in on the device, the device passes
/// to them; it is removed when the user signs out there, or when the push service says the token is no longer valid.
/// </summary>
/// <remarks>A token is unique among the devices, which the persistence layer keeps so.</remarks>
public sealed class PushDevice : BaseEntity
{
    /// <summary>The longest token kept; the tokens of Firebase Cloud Messaging are far shorter.</summary>
    public const int MaxTokenLength = 4096;

    /// <summary>Gets the identifier of the user the device notifies.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the token of the device in the push service.</summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>Gets the push service the device is reached through.</summary>
    public PushPlatform Platform { get; private set; }

    /// <summary>Gets the UTC time the device was last registered, by the user it notifies or another.</summary>
    public DateTime RegisteredAtUtc { get; private set; }

    private PushDevice(Guid id, string token, PushPlatform platform) : base(id)
    {
        Token = token;
        Platform = platform;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private PushDevice()
    {
    }

    /// <summary>Registers a device for a user.</summary>
    /// <param name="userId">The user the device notifies.</param>
    /// <param name="token">The token of the device in the push service.</param>
    /// <param name="platform">The push service.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The device, or the first validation failure encountered.</returns>
    public static Result<PushDevice> Register(Guid userId, string? token, PushPlatform platform, DateTime utcNow)
    {
        var userResult = Guard.NotEmpty(userId, DomainErrors.PushDevice.UserIdEmpty);
        if (userResult.IsFailure)
            return Result.Failure<PushDevice>(userResult.Error);

        var tokenResult = Guard.RequiredText(
            token, MaxTokenLength, DomainErrors.PushDevice.TokenEmpty, DomainErrors.PushDevice.TokenTooLong);
        if (tokenResult.IsFailure)
            return Result.Failure<PushDevice>(tokenResult.Error);

        var platformResult = Guard.DefinedEnum(platform, DomainErrors.PushDevice.PlatformNotDefined);
        if (platformResult.IsFailure)
            return Result.Failure<PushDevice>(platformResult.Error);

        var device = new PushDevice(Guid.CreateVersion7(), tokenResult.Value, platform);
        device.AssignTo(userId, utcNow);

        return device;
    }

    /// <summary>
    /// Registers the device again, for the user signed in on it now, who may be another than before.
    /// </summary>
    /// <param name="userId">The user the device notifies from now on.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>Success, or a failure for an empty user.</returns>
    public Result Reassign(Guid userId, DateTime utcNow)
    {
        var userResult = Guard.NotEmpty(userId, DomainErrors.PushDevice.UserIdEmpty);
        if (userResult.IsFailure)
            return Result.Failure(userResult.Error);

        AssignTo(userId, utcNow);

        return Result.Success();
    }

    private void AssignTo(Guid userId, DateTime utcNow)
    {
        UserId = userId;
        RegisteredAtUtc = utcNow;
    }
}

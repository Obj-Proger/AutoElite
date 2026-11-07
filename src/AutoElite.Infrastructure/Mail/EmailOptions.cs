using AutoElite.Infrastructure.Common;

namespace AutoElite.Infrastructure.Mail;

/// <summary>Where the queued emails go.</summary>
public enum EmailDeliveryMode
{
    /// <summary>To the school's SMTP server.</summary>
    Smtp,

    /// <summary>To <c>.eml</c> files in a folder, for development and testing, where no email must leave.</summary>
    PickupDirectory
}

/// <summary>
/// How the queued emails are sent, from the <see cref="SectionName"/> configuration section. The SMTP password is a
/// secret: it is set with user secrets locally and with an environment variable or a secret store when deployed
/// (<c>Email__Password</c>), never in a committed file.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Email";

    /// <summary>Gets or sets where the emails go.</summary>
    public EmailDeliveryMode Mode { get; set; } = EmailDeliveryMode.Smtp;

    /// <summary>Gets or sets the host name of the SMTP server.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the port of the SMTP server: 587 for STARTTLS, 465 for TLS from the start.</summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets whether the connection is encrypted from the start (port 465) rather than upgraded with STARTTLS;
    /// a connection that cannot be encrypted is refused either way.
    /// </summary>
    public bool UseTlsFromStart { get; set; }

    /// <summary>Gets or sets the user name of the SMTP account, if the server asks for one.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the password of the SMTP account; a secret.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the address the emails come from.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the name the emails come from, such as the school's.</summary>
    public string? FromName { get; set; }

    /// <summary>Gets or sets the folder of the <see cref="EmailDeliveryMode.PickupDirectory"/> mode.</summary>
    public string? PickupDirectory { get; set; }

    /// <summary>Gets or sets how long the delivery waits after finding no email to send.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the sends of an email before it is given up.</summary>
    public int MaxAttempts { get; set; } = 8;

    /// <summary>Gets or sets the pause after the first failure; every further failure doubles it.</summary>
    public TimeSpan FirstRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the longest pause between sends of a failing email.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(2);

    /// <summary>Gets the pause after the given number of failed sends.</summary>
    /// <param name="attempts">The failed sends so far, the latest included; at least 1.</param>
    public TimeSpan RetryDelayAfter(int attempts) => RetryDelays.After(attempts, FirstRetryDelay, MaxRetryDelay);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (Email.Create(FromAddress).IsFailure)
            yield return $"{SectionName}:FromAddress must be an email address.";

        if (Mode == EmailDeliveryMode.Smtp)
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                yield return $"{SectionName}:Host must name the SMTP server, or {SectionName}:Mode be PickupDirectory "
                    + "for development.";
            }

            if (Port is < 1 or > 65535)
                yield return $"{SectionName}:Port must be from 1 to 65535.";

            if (string.IsNullOrEmpty(UserName) != string.IsNullOrEmpty(Password))
            {
                yield return $"{SectionName}:UserName and {SectionName}:Password are set together. Set the password with "
                    + $"dotnet user-secrets locally or the {SectionName}__Password environment variable when deployed.";
            }
        }
        else if (string.IsNullOrWhiteSpace(PickupDirectory))
        {
            yield return $"{SectionName}:PickupDirectory must name a folder in the PickupDirectory mode.";
        }

        if (PollingInterval <= TimeSpan.Zero)
            yield return $"{SectionName}:PollingInterval must be positive.";

        if (MaxAttempts < 1)
            yield return $"{SectionName}:MaxAttempts must be at least 1.";

        if (FirstRetryDelay <= TimeSpan.Zero || MaxRetryDelay < FirstRetryDelay)
            yield return $"{SectionName}:FirstRetryDelay must be positive and not longer than {SectionName}:MaxRetryDelay.";
    }
}

namespace AutoElite.Infrastructure.Mail;

/// <summary>
/// An email waiting for delivery, stored in the same transaction as the change that asked for it, so that it is sent
/// if and only if the change is committed. The delivery job sends it after the commit and retries after failures.
/// </summary>
/// <remarks>
/// Emails such as invitations and password resets carry one-time links, so the delivery clears the body once the
/// email is sent; a waiting body is in the database only for as long as delivery takes.
/// </remarks>
internal sealed class QueuedEmail
{
    /// <summary>The longest subject line.</summary>
    public const int MaxSubjectLength = 500;

    /// <summary>The longest error kept from a failed delivery.</summary>
    public const int MaxErrorLength = 4000;

    private QueuedEmail()
    {
    }

    /// <summary>Gets the identifier of the email, which also orders emails by creation.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the recipient's address.</summary>
    public Email To { get; private set; } = null!;

    /// <summary>Gets the subject line.</summary>
    public string Subject { get; private set; } = null!;

    /// <summary>Gets the plain-text body, or <see langword="null"/> once the email is sent.</summary>
    public string? Body { get; private set; }

    /// <summary>Gets the UTC time the email was queued.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the email was sent, or <see langword="null"/> while it waits.</summary>
    public DateTime? SentAtUtc { get; private set; }

    /// <summary>Gets the number of failed deliveries.</summary>
    public int Attempts { get; private set; }

    /// <summary>Gets the UTC time before which the email is not delivered: the pause after a failure.</summary>
    public DateTime NextAttemptAtUtc { get; private set; }

    /// <summary>Gets the error of the last failed delivery, for whoever investigates an email that keeps failing.</summary>
    public string? LastError { get; private set; }

    /// <summary>Marks the email sent and clears its body, which may hold one-time links.</summary>
    /// <param name="utcNow">The current UTC time.</param>
    public void MarkSent(DateTime utcNow)
    {
        SentAtUtc = utcNow;
        Body = null;
    }

    /// <summary>Queues an email.</summary>
    /// <param name="message">The email.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <exception cref="ArgumentOutOfRangeException">The subject is longer than <see cref="MaxSubjectLength"/>.</exception>
    public static QueuedEmail Create(EmailMessage message, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(message.Subject.Length, MaxSubjectLength, nameof(message));

        return new QueuedEmail
        {
            Id = Guid.CreateVersion7(),
            To = message.To,
            Subject = message.Subject,
            Body = message.Body,
            CreatedAtUtc = utcNow,
            NextAttemptAtUtc = utcNow
        };
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoElite.Infrastructure.Mail;

/// <summary>Sends one email; a failure is thrown.</summary>
internal interface IEmailSender
{
    /// <summary>Sends an email.</summary>
    /// <param name="email">The email, with its body.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task SendAsync(QueuedEmail email, CancellationToken cancellationToken);
}

/// <summary>
/// Builds the MIME message of a queued email. The subject is made one line, so that it cannot start a header of its
/// own, and MimeKit encodes it.
/// </summary>
internal static class EmailMessages
{
    private static readonly char[] _lineBreaks = ['\r', '\n'];

    /// <summary>Builds the message of an email from the configured sender.</summary>
    /// <param name="email">The email.</param>
    /// <param name="options">The settings with the sender's address and name.</param>
    public static MimeMessage Compose(QueuedEmail email, EmailOptions options)
    {
        var message = new MimeMessage
        {
            Subject = string.Join(' ', email.Subject.Split(_lineBreaks, StringSplitOptions.RemoveEmptyEntries)),
            Body = new TextPart(MimeKit.Text.TextFormat.Plain) { Text = email.Body ?? string.Empty }
        };
        message.From.Add(new MailboxAddress(options.FromName ?? string.Empty, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.To.Value));
        message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId(MailboxAddress.Parse(options.FromAddress).Domain);

        return message;
    }
}

/// <summary>
/// Sends emails to the SMTP server over an encrypted connection, one connection per email, which suits the volume of
/// a school.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseTlsFromStart ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
            cancellationToken);
        if (!string.IsNullOrEmpty(settings.UserName))
            await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(EmailMessages.Compose(email, settings), cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

/// <summary>Writes emails to <c>.eml</c> files, named by the email's identifier, instead of sending them.</summary>
internal sealed class PickupDirectoryEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Directory.CreateDirectory(settings.PickupDirectory!);
        var path = Path.Combine(settings.PickupDirectory!, $"{email.Id:N}.eml");
        await EmailMessages.Compose(email, settings).WriteToAsync(path, cancellationToken);
    }
}

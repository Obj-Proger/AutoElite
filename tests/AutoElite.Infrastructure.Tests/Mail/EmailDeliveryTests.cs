using System.Collections.Concurrent;
using AutoElite.Infrastructure.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoElite.Infrastructure.Tests.Mail;

public sealed class EmailDeliveryTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static EmailOptions Settings(int maxAttempts = 8) =>
        new()
        {
            Mode = EmailDeliveryMode.PickupDirectory,
            PickupDirectory = "unused",
            FromAddress = "school@example.com",
            FromName = "Автошкола",
            MaxAttempts = maxAttempts
        };

    private static QueuedEmail Queued(string subject, int minutes) =>
        QueuedEmail.Create(
            new EmailMessage(Email.Create($"{Guid.NewGuid():N}@example.com").Value, subject, "Ссылка: https://example.com/t"),
            _now.AddMinutes(minutes));

    /// <summary>Empties the queue, which the tests of the class share, and queues the emails.</summary>
    private async Task StoreAsync(params QueuedEmail[] emails)
    {
        await using var context = database.CreateContext();
        await context.Set<QueuedEmail>().ExecuteDeleteAsync(CancellationToken);
        context.AddRange(emails);
        await context.SaveChangesAsync(CancellationToken);
    }

    private async Task<QueuedEmail> LoadAsync(Guid emailId)
    {
        await using var context = database.CreateContext();

        return await context.Set<QueuedEmail>().AsNoTracking().SingleAsync(email => email.Id == emailId, CancellationToken);
    }

    private static EmailDeliveryProcessor Processor(
        ServiceProvider services, RecordingSender sender, DateTime utcNow, int maxAttempts = 8) =>
        new(
            services.GetRequiredService<IServiceScopeFactory>(), sender, new FixedClock(utcNow),
            Options.Create(Settings(maxAttempts)), NullLogger<EmailDeliveryProcessor>.Instance);

    [Fact]
    public async Task SendNext_DueEmails_SendsThemOldestFirstAndClearsTheirBodies()
    {
        var first = Queued("Первое", 1);
        var second = Queued("Второе", 2);
        var later = Queued("Позже", 60);
        await StoreAsync(second, later, first);
        await using var services = database.CreateServices();
        var sender = new RecordingSender();
        var processor = Processor(services, sender, _now.AddMinutes(5));

        while (await processor.SendNextAsync(CancellationToken))
            await Task.Yield();

        sender.Sent.Select(email => email.Subject).Should().Equal("Первое", "Второе");
        sender.Sent[0].Body.Should().Be("Ссылка: https://example.com/t");
        var sent = await LoadAsync(first.Id);
        sent.SentAtUtc.Should().Be(_now.AddMinutes(5));
        sent.Body.Should().BeNull();
        (await LoadAsync(later.Id)).SentAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task SendNext_FailingServer_KeepsTheEmailAndRetriesAfterAPause()
    {
        var email = Queued("Приглашение", 0);
        await StoreAsync(email);
        await using var services = database.CreateServices();
        var sender = new RecordingSender(failures: 1);
        var at = _now.AddMinutes(1);

        (await Processor(services, sender, at).SendNextAsync(CancellationToken)).Should().BeTrue();
        var failed = await LoadAsync(email.Id);
        (await Processor(services, sender, at.AddSeconds(59)).SendNextAsync(CancellationToken)).Should().BeFalse();
        (await Processor(services, sender, at.AddMinutes(1)).SendNextAsync(CancellationToken)).Should().BeTrue();

        failed.SentAtUtc.Should().BeNull();
        failed.Attempts.Should().Be(1);
        failed.NextAttemptAtUtc.Should().Be(at.AddMinutes(1));
        failed.LastError.Should().Contain(RecordingSender.FailureText);
        failed.Body.Should().NotBeNull();
        (await LoadAsync(email.Id)).SentAtUtc.Should().Be(at.AddMinutes(1));
        sender.Sent.Should().ContainSingle();
    }

    [Fact]
    public async Task SendNext_EmailThatFailedTooOften_IsGivenUp()
    {
        var email = Queued("Приглашение", 0);
        await StoreAsync(email);
        await using var services = database.CreateServices();
        var sender = new RecordingSender(failures: int.MaxValue);

        (await Processor(services, sender, _now.AddDays(1), maxAttempts: 1).SendNextAsync(CancellationToken)).Should().BeTrue();
        (await Processor(services, sender, _now.AddDays(2), maxAttempts: 1).SendNextAsync(CancellationToken)).Should().BeFalse();

        (await LoadAsync(email.Id)).Attempts.Should().Be(1);
    }

    [Fact]
    public async Task SendNext_TwoDeliveriesAtOnce_SendEveryEmailOnce()
    {
        var emails = Enumerable.Range(0, 20).Select(minutes => Queued($"Письмо {minutes}", minutes)).ToArray();
        await StoreAsync(emails);
        await using var services = database.CreateServices();
        var sender = new RecordingSender();

        async Task DrainAsync()
        {
            var processor = Processor(services, sender, _now.AddHours(1));
            while (await processor.SendNextAsync(CancellationToken))
                await Task.Yield();
        }

        await Task.WhenAll(DrainAsync(), DrainAsync());

        sender.Sent.Should().HaveCount(emails.Length);
        sender.Sent.Select(email => email.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Compose_EmailWithALineBreakInTheSubject_AddsNoHeader()
    {
        var email = Queued("Тема\r\nBcc: attacker@example.com", 0);

        using var stream = new MemoryStream();
        await EmailMessages.Compose(email, Settings()).WriteToAsync(stream, CancellationToken);
        stream.Position = 0;
        using var parsed = await MimeMessage.LoadAsync(stream, CancellationToken);

        parsed.Bcc.Count.Should().Be(0);
        parsed.Subject.Should().Be("Тема Bcc: attacker@example.com");
        parsed.To.Mailboxes.Single().Address.Should().Be(email.To.Value);
        parsed.From.Mailboxes.Single().Address.Should().Be("school@example.com");
        parsed.From.Mailboxes.Single().Name.Should().Be("Автошкола");
        parsed.TextBody.Should().Contain("https://example.com/t");
    }

    [Fact]
    public async Task PickupDirectory_WritesTheEmailToAFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"autoelite-mail-{Guid.NewGuid():N}");
        try
        {
            var settings = Settings();
            settings.PickupDirectory = folder;
            var email = Queued("Приглашение", 0);

            await new PickupDirectoryEmailSender(Options.Create(settings)).SendAsync(email, CancellationToken);

            using var parsed = await MimeMessage.LoadAsync(Path.Combine(folder, $"{email.Id:N}.eml"), CancellationToken);
            parsed.Subject.Should().Be("Приглашение");
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Options_ReportWhatIsMissingForTheMode()
    {
        var smtp = new EmailOptions { FromAddress = "school@example.com", UserName = "school" };
        var pickup = new EmailOptions { Mode = EmailDeliveryMode.PickupDirectory, FromAddress = "not an address" };

        smtp.Problems().Should().Contain(problem => problem.Contains("Host")).And.Contain(problem => problem.Contains("Password"));
        pickup.Problems().Should().Contain(problem => problem.Contains("PickupDirectory"))
            .And.Contain(problem => problem.Contains("FromAddress"));
        Settings().Problems().Should().BeEmpty();
    }

    /// <summary>Records the emails as they were sent, after failing as often as told.</summary>
    /// <param name="failures">The sends that fail before the first that succeeds.</param>
    private sealed class RecordingSender(int failures = 0) : IEmailSender
    {
        public const string FailureText = "The server refused the email on purpose.";

        private readonly ConcurrentQueue<(Guid Id, string Subject, string? Body)> _sent = new();

        private int _failuresLeft = failures;

        public IReadOnlyList<(Guid Id, string Subject, string? Body)> Sent => [.. _sent];

        public Task SendAsync(QueuedEmail email, CancellationToken cancellationToken)
        {
            if (Interlocked.Decrement(ref _failuresLeft) >= 0)
                throw new InvalidOperationException(FailureText);

            _sent.Enqueue((email.Id, email.Subject, email.Body));

            return Task.CompletedTask;
        }
    }
}

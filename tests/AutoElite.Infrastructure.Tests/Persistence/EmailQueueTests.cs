using AutoElite.Infrastructure.Mail;
using Microsoft.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class EmailQueueTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static EmailMessage Message(string to) =>
        new(Email.Create(to).Value, "Приглашение в АИС", "Перейдите по ссылке, чтобы задать пароль.");

    [Fact]
    public async Task Enqueue_ThenSave_StoresTheEmailForDelivery()
    {
        var address = $"Student.{Guid.NewGuid():N}@Example.com";
        await using (var context = database.CreateContext())
        {
            await new EmailQueue(context, FixedClock.Default).EnqueueAsync(Message(address), CancellationToken);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var to = Email.Create(address).Value;
        var stored = await reader.Set<QueuedEmail>().SingleAsync(email => email.To == to, CancellationToken);
        stored.To.Value.Should().Be(address.ToLowerInvariant());
        stored.Subject.Should().Be("Приглашение в АИС");
        stored.Body.Should().Be("Перейдите по ссылке, чтобы задать пароль.");
        stored.CreatedAtUtc.Should().Be(FixedClock.Default.UtcNow);
        stored.NextAttemptAtUtc.Should().Be(FixedClock.Default.UtcNow);
        stored.SentAtUtc.Should().BeNull();
        stored.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task Enqueue_WithoutSaving_StoresNothing()
    {
        var address = $"student.{Guid.NewGuid():N}@example.com";
        await using (var context = database.CreateContext())
            await new EmailQueue(context, FixedClock.Default).EnqueueAsync(Message(address), CancellationToken);

        await using var reader = database.CreateContext();
        var to = Email.Create(address).Value;
        var stored = await reader.Set<QueuedEmail>().AnyAsync(email => email.To == to, CancellationToken);
        stored.Should().BeFalse();
    }
}

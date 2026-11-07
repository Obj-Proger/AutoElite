using AutoElite.Application.Common.Messaging;
using AutoElite.Application.Notifications.SendReminders;
using AutoElite.Application.StaffTasks.RemindOverdueStaffTasks;
using AutoElite.Domain.Common;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.BackgroundJobs;
using AutoElite.Infrastructure.Mail;
using AutoElite.Infrastructure.Outbox;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.BackgroundJobs;

public sealed class BackgroundJobsTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static readonly string[] _jobIds =
    [
        RecurringJobsRegistrar.SendRemindersJobId,
        RecurringJobsRegistrar.RemindOverdueStaffTasksJobId,
        RecurringJobsRegistrar.CleanUpJobId
    ];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SystemJobs_SendTheirCommands_AndThrowWhenOneFails()
    {
        var dispatcher = new RecordingDispatcher();
        var jobs = new SystemJobs(dispatcher);

        await jobs.SendRemindersAsync(CancellationToken);
        await jobs.RemindOverdueStaffTasksAsync(CancellationToken);
        dispatcher.Result = Result.Failure(Error.Failure("Test.Failed", "The command failed."));
        var failing = () => jobs.SendRemindersAsync(CancellationToken);

        dispatcher.Sent.Should().HaveCount(2);
        dispatcher.Sent[0].Should().BeOfType<SendRemindersCommand>();
        dispatcher.Sent[1].Should().BeOfType<RemindOverdueStaffTasksCommand>();
        await failing.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Test.Failed*");
    }

    [Fact]
    public async Task CleanUp_RemovesOnlyDeliveredMessagesAndSentEmailsPastTheRetention()
    {
        DocumentDeletedDomainEvent Deleted() =>
            new(Guid.CreateVersion7(), EntityKind.User, Guid.CreateVersion7(), $"documents/{Guid.NewGuid():N}", _now);

        var oldDelivered = OutboxMessage.Create(Deleted());
        oldDelivered.MarkProcessed(_now.AddDays(-31));
        var recentDelivered = OutboxMessage.Create(Deleted());
        recentDelivered.MarkProcessed(_now.AddDays(-29));
        var waiting = OutboxMessage.Create(Deleted());
        var message = new EmailMessage(Email.Create("student@example.com").Value, "Тема", "Текст");
        var oldSent = QueuedEmail.Create(message, _now.AddDays(-40));
        oldSent.MarkSent(_now.AddDays(-31));
        var unsent = QueuedEmail.Create(message, _now.AddDays(-40));
        await using (var context = database.CreateContext())
        {
            context.AddRange(oldDelivered, recentDelivered, waiting, oldSent, unsent);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var context = database.CreateContext())
        {
            var jobs = new MaintenanceJobs(context, FixedClock.Default, Options.Create(new BackgroundJobsOptions()));
            await jobs.CleanUpAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        (await reader.Set<OutboxMessage>().Select(found => found.Id).ToListAsync(CancellationToken))
            .Should().NotContain(oldDelivered.Id).And.Contain(recentDelivered.Id).And.Contain(waiting.Id);
        (await reader.Set<QueuedEmail>().Select(found => found.Id).ToListAsync(CancellationToken))
            .Should().NotContain(oldSent.Id).And.Contain(unsent.Id);
        oldSent.Body.Should().BeNull();
    }

    [Fact]
    public async Task Registrar_SchedulesTheRecurringJobsInHangfiresStorage()
    {
        await using var services = database.CreateServices();
        // Made here rather than taken from the hosted services, whose list would also make the Hangfire server.
        var registrar = new RecurringJobsRegistrar(services.GetRequiredService<IRecurringJobManager>());

        await registrar.StartAsync(CancellationToken);
        await registrar.StartAsync(CancellationToken);

        using var connection = services.GetRequiredService<JobStorage>().GetConnection();
        var scheduled = connection.GetRecurringJobs().ToDictionary(job => job.Id);
        scheduled.Keys.Should().BeEquivalentTo(_jobIds);
        scheduled[RecurringJobsRegistrar.SendRemindersJobId].Cron.Should().Be(Cron.MinuteInterval(5));
        scheduled[RecurringJobsRegistrar.SendRemindersJobId].Job.Method.Name.Should().Be(nameof(SystemJobs.SendRemindersAsync));
        scheduled[RecurringJobsRegistrar.CleanUpJobId].Job.Type.Should().Be<MaintenanceJobs>();
    }

    [Fact]
    public void Options_InvalidSettings_AreReported()
    {
        var options = new BackgroundJobsOptions { WorkerCount = 0, RetentionPeriod = TimeSpan.FromHours(1) };

        options.Problems().Should().HaveCount(2);
        new BackgroundJobsOptions().Problems().Should().BeEmpty();
    }

    /// <summary>Records the commands sent and answers each with <see cref="Result"/>.</summary>
    private sealed class RecordingDispatcher : IDispatcher
    {
        public List<object> Sent { get; } = [];

        public Result Result { get; set; } = Result.Success();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);

            return Task.FromResult((TResponse)(object)Result);
        }
    }
}

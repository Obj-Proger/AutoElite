using System.Collections.Concurrent;
using AutoElite.Application.Common.Events;
using AutoElite.Domain.Common;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure.Tests.Outbox;

public sealed class OutboxProcessorTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DocumentDeletedDomainEvent Deleted(int minutes) =>
        new(Guid.CreateVersion7(), EntityKind.User, Guid.CreateVersion7(), $"documents/{Guid.NewGuid():N}", _now.AddMinutes(minutes));

    /// <summary>Empties the outbox, which the tests of the class share, and stores the events in it.</summary>
    private async Task<List<Guid>> StoreAsync(params IDomainEvent[] events)
    {
        await using var context = database.CreateContext();
        await context.Set<OutboxMessage>().ExecuteDeleteAsync(CancellationToken);
        var messages = events.Select(OutboxMessage.Create).ToList();
        context.AddRange(messages);
        await context.SaveChangesAsync(CancellationToken);

        return [.. messages.Select(message => message.Id)];
    }

    private async Task<OutboxMessage> LoadAsync(Guid messageId)
    {
        await using var context = database.CreateContext();

        return await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(message => message.Id == messageId, CancellationToken);
    }

    /// <summary>The services of a host whose publisher is the recorder, on a clock that stands still.</summary>
    private ServiceProvider Services(RecordingPublisher publisher, DateTime utcNow, int maxAttempts = 10) =>
        database.CreateServices(configure: services =>
        {
            services.AddSingleton(publisher);
            services.AddScoped<IDomainEventPublisher, RecordingPublisher.Scoped>();
            services.AddSingleton<TimeProvider>(new FixedClock(utcNow));
            services.Configure<OutboxOptions>(options =>
            {
                options.BatchSize = 5;
                options.MaxAttempts = maxAttempts;
            });
        });

    private async Task<int> ProcessAsync(RecordingPublisher publisher, DateTime utcNow, int maxAttempts = 10)
    {
        await using var services = Services(publisher, utcNow, maxAttempts);

        return await services.GetRequiredService<OutboxProcessor>().ProcessAsync(CancellationToken);
    }

    [Fact]
    public async Task Process_DueMessages_DeliversThemOldestFirstAndMarksThemProcessed()
    {
        var ids = await StoreAsync(Deleted(2), Deleted(1), Deleted(60));
        var publisher = new RecordingPublisher();
        var at = _now.AddMinutes(5);

        var taken = await ProcessAsync(publisher, at);
        var again = await ProcessAsync(publisher, at);

        taken.Should().Be(2);
        again.Should().Be(0);
        publisher.Delivered.Select(delivered => delivered.OccurredOnUtc).Should().Equal(_now.AddMinutes(1), _now.AddMinutes(2));
        (await LoadAsync(ids[0])).ProcessedOnUtc.Should().Be(at);
        (await LoadAsync(ids[2])).ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Process_FailingHandler_KeepsTheMessageAndRetriesAfterAPauseThatDoubles()
    {
        var ids = await StoreAsync(Deleted(0));
        var publisher = new RecordingPublisher(failures: 2);
        var at = _now.AddMinutes(1);

        (await ProcessAsync(publisher, at)).Should().Be(1);
        var failed = await LoadAsync(ids[0]);
        (await ProcessAsync(publisher, at.AddSeconds(29))).Should().Be(0);
        (await ProcessAsync(publisher, at.AddSeconds(30))).Should().Be(1);
        var failedTwice = await LoadAsync(ids[0]);
        (await ProcessAsync(publisher, at.AddSeconds(90))).Should().Be(1);

        failed.ProcessedOnUtc.Should().BeNull();
        failed.Attempts.Should().Be(1);
        failed.NextAttemptOnUtc.Should().Be(at.AddSeconds(30));
        failed.LastError.Should().Contain(RecordingPublisher.FailureText);
        failedTwice.Attempts.Should().Be(2);
        failedTwice.NextAttemptOnUtc.Should().Be(at.AddSeconds(90));
        (await LoadAsync(ids[0])).ProcessedOnUtc.Should().Be(at.AddSeconds(90));
        publisher.Delivered.Should().ContainSingle();
    }

    [Fact]
    public async Task Process_MessageThatFailedTooOften_IsGivenUp()
    {
        var ids = await StoreAsync(Deleted(0));
        var publisher = new RecordingPublisher(failures: int.MaxValue);
        var at = _now.AddDays(1);

        (await ProcessAsync(publisher, at, maxAttempts: 1)).Should().Be(1);
        (await ProcessAsync(publisher, at.AddDays(1), maxAttempts: 1)).Should().Be(0);

        var givenUp = await LoadAsync(ids[0]);
        givenUp.Attempts.Should().Be(1);
        givenUp.ProcessedOnUtc.Should().BeNull();
        givenUp.LastError.Should().Contain(RecordingPublisher.FailureText);
    }

    [Fact]
    public async Task Process_TwoProcessorsAtOnce_DeliverEveryMessageOnce()
    {
        var ids = await StoreAsync([.. Enumerable.Range(0, 30).Select(Deleted)]);
        var publisher = new RecordingPublisher();
        await using var first = Services(publisher, _now.AddHours(1));
        await using var second = Services(publisher, _now.AddHours(1));

        static async Task DrainAsync(ServiceProvider services)
        {
            var processor = services.GetRequiredService<OutboxProcessor>();
            while (await processor.ProcessAsync(CancellationToken) > 0)
                await Task.Yield();
        }

        await Task.WhenAll(DrainAsync(first), DrainAsync(second));

        publisher.Delivered.Should().HaveCount(ids.Count);
        publisher.Delivered.Select(delivered => delivered.StorageKey).Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Records the delivered events, after failing as often as told, and saves as the real publisher does; the scopes
    /// of a test share one recorder through <see cref="Scoped"/>.
    /// </summary>
    /// <param name="failures">The deliveries that fail before the first that succeeds.</param>
    private sealed class RecordingPublisher(int failures = 0)
    {
        public const string FailureText = "The handler failed on purpose.";

        private readonly ConcurrentQueue<DocumentDeletedDomainEvent> _delivered = new();

        private int _failuresLeft = failures;

        public IReadOnlyList<DocumentDeletedDomainEvent> Delivered => [.. _delivered];

        public sealed class Scoped(RecordingPublisher recorder, IUnitOfWork unitOfWork) : IDomainEventPublisher
        {
            public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            {
                if (Interlocked.Decrement(ref recorder._failuresLeft) >= 0)
                    throw new InvalidOperationException(FailureText);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                recorder._delivered.Enqueue((DocumentDeletedDomainEvent)domainEvent);
            }
        }
    }
}

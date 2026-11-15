using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.API.DemoData;

/// <summary>
/// The clock of a host with demo data: the system clock, which the demo seeder turns back while it creates the past
/// of the school, such as lessons already held, so that every step passes the same commands and rules as it would
/// have then; it is the system clock again before the host serves requests.
/// </summary>
/// <param name="system">The clock it follows.</param>
internal sealed class DemoClock(TimeProvider system) : TimeProvider
{
    // The difference from the clock it follows, in ticks, read by the workers of the host too; zero but while the
    // seeder creates the past.
    private long _offsetTicks;

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => system.LocalTimeZone;

    /// <inheritdoc />
    public override long TimestampFrequency => system.TimestampFrequency;

    /// <summary>
    /// Registers the clock as the <see cref="TimeProvider"/> of the host, in place of the system clock.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton(new DemoClock(TimeProvider.System));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(provider => provider.GetRequiredService<DemoClock>()));
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => system.GetUtcNow().AddTicks(Volatile.Read(ref _offsetTicks));

    /// <inheritdoc />
    public override long GetTimestamp() => system.GetTimestamp();

    /// <summary>Makes the clock tell a moment of the past, from which it runs on.</summary>
    /// <param name="moment">The moment, in UTC.</param>
    public void Set(DateTime moment) =>
        Volatile.Write(ref _offsetTicks, (new DateTimeOffset(moment, TimeSpan.Zero) - system.GetUtcNow()).Ticks);

    /// <summary>Makes the clock tell the present again.</summary>
    public void Reset() => Volatile.Write(ref _offsetTicks, 0);
}

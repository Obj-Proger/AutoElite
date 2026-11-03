namespace AutoElite.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_NoClockRegistered_RegistersTheSystemClock()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddApplication_ClockAlreadyRegistered_KeepsIt()
    {
        var services = new ServiceCollection();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero));
        services.AddSingleton<TimeProvider>(clock);

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(clock);
    }
}

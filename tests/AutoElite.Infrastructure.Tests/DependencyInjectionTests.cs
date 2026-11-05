using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(TestCurrentUser.System);
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    [Fact]
    public void AddInfrastructure_WithConnectionString_RegistersTheUnitOfWork()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=autoelite"
            })
            .Build();

        using var provider = Services().AddInfrastructure(configuration).BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Model.FindEntityType(typeof(Branch)).Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_Throws()
    {
        var add = () => Services().AddInfrastructure(new ConfigurationBuilder().Build());

        add.Should().Throw<InvalidOperationException>().WithMessage($"*{DependencyInjection.ConnectionStringName}*");
    }
}

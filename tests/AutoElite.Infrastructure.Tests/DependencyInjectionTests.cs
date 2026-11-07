using AutoElite.Application.Analytics;
using AutoElite.Application.Questions.ImportQuestions;
using AutoElite.Infrastructure.Import;
using AutoElite.Infrastructure.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    private static IConfiguration Configuration(bool withChatKeys) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=autoelite"
            })
            .AddInMemoryCollection(withChatKeys ? TestChatEncryption.Configuration : new Dictionary<string, string?>())
            .Build();

    [Fact]
    public void AddInfrastructure_WithConnectionString_RegistersTheUnitOfWork()
    {
        var configuration = Configuration(withChatKeys: true);

        using var provider = Services().AddInfrastructure(configuration).BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>().Model.FindEntityType(typeof(Branch)).Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IQuestionFileReader>().Should().BeOfType<QuestionFileReader>();
        scope.ServiceProvider.GetRequiredService<IReportRenderer>().Should().BeOfType<ReportRenderer>();
    }

    [Fact]
    public void AddInfrastructure_WithoutChatKeys_FailsToBuildTheDatabaseContext()
    {
        var configuration = Configuration(withChatKeys: false);

        using var provider = Services().AddInfrastructure(configuration).BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var resolve = () => scope.ServiceProvider.GetRequiredService<AutoEliteDbContext>();

        resolve.Should().Throw<OptionsValidationException>().WithMessage("*ChatEncryption:Keys*");
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_Throws()
    {
        var add = () => Services().AddInfrastructure(new ConfigurationBuilder().Build());

        add.Should().Throw<InvalidOperationException>().WithMessage($"*{DependencyInjection.ConnectionStringName}*");
    }
}

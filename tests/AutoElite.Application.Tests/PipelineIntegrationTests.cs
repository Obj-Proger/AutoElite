using AutoElite.Application.Common.Abstractions;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests;

public class PipelineIntegrationTests
{
    private readonly FakeUnitOfWork _unitOfWork = new();

    private (IDispatcher Dispatcher, CallJournal Journal) CreateDispatcher(FakeCurrentUser? currentUser = null)
    {
        var journal = new CallJournal();
        var services = new ServiceCollection();
        services.AddSingleton(journal);
        services.AddSingleton<ICurrentUser>(currentUser ?? FakeCurrentUser.SignedIn(UserRole.Student));
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton(typeof(ILogger<>), typeof(ListLogger<>));
        services.AddApplication();
        services.AddScoped<IRequestHandler<TestCommand, Result>, TestCommandHandler>();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();

        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IDispatcher>(), journal);
    }

    [Fact]
    public async Task Send_ValidCommand_RunsHandler()
    {
        var (dispatcher, journal) = CreateDispatcher();

        var result = await dispatcher.Send(new TestCommand("name"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        journal.Entries.Should().Equal("handler");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_InvalidCommand_ReturnsValidationErrorAndSkipsHandler()
    {
        var (dispatcher, journal) = CreateDispatcher();

        var result = await dispatcher.Send(new TestCommand(string.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
        result.Error.FieldErrors.Should().Equal(new FieldError("Name", "Test.NameEmpty", "Name is required."));
        journal.Entries.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Send_InvalidCommandFromAnonymousCaller_ReturnsUnauthenticatedBeforeValidation()
    {
        var (dispatcher, journal) = CreateDispatcher(FakeCurrentUser.Anonymous());

        var result = await dispatcher.Send(new TestCommand(string.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.Auth.Unauthenticated);
        journal.Entries.Should().BeEmpty();
    }

    [Fact]
    public void AddApplication_RegistersDispatcher()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(ListLogger<>));
        services.AddApplication();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDispatcher>().Should().NotBeNull();
    }
}

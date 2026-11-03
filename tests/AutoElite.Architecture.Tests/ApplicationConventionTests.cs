namespace AutoElite.Architecture.Tests;

public class ApplicationConventionTests
{
    [Fact]
    public void RequestHandlers_AreInternalSealedAndNamedHandler()
    {
        var violating = Layers.ClassesImplementing(Layers.Application, typeof(IRequestHandler<,>))
            .Where(type => type.IsVisible || !type.IsSealed || !type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(type => type.Name);

        violating.Should().BeEmpty("handlers are reached only through the dispatcher");
    }

    [Fact]
    public void Commands_AreNamedCommand()
    {
        var commands = Layers.Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && (typeof(ICommand).IsAssignableFrom(type) || Implements(type, typeof(ICommand<>))));

        commands.Where(type => !type.Name.EndsWith("Command", StringComparison.Ordinal))
            .Select(type => type.Name)
            .Should().BeEmpty();
    }

    [Fact]
    public void Queries_AreNamedQuery()
    {
        var queries = Layers.ClassesImplementing(Layers.Application, typeof(IQuery<>));

        queries.Where(type => !type.Name.EndsWith("Query", StringComparison.Ordinal))
            .Select(type => type.Name)
            .Should().BeEmpty();
    }

    [Fact]
    public void Validators_AreInternalSealedAndNamedValidator()
    {
        var violating = Layers.ClassesImplementing(Layers.Application, typeof(IValidator<>))
            .Where(type => type.IsVisible || !type.IsSealed || !type.Name.EndsWith("Validator", StringComparison.Ordinal))
            .Select(type => type.Name);

        violating.Should().BeEmpty();
    }

    private static bool Implements(Type type, Type openGenericInterface) =>
        type.GetInterfaces().Any(implemented =>
            implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openGenericInterface);
}

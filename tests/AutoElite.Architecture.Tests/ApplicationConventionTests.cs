using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Pagination;

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
    public void DomainEventHandlers_AreInternalSealedAndNamedHandler()
    {
        var violating = Layers.ClassesImplementing(Layers.Application, typeof(IDomainEventHandler<>))
            .Where(type => type.IsVisible || !type.IsSealed || !type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(type => type.Name);

        violating.Should().BeEmpty("event handlers are reached only through the domain event publisher");
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

    [Fact]
    public void Requests_HaveAtMostOneAuthorizationAttribute()
    {
        var contradictory = Layers.ClassesImplementing(Layers.Application, typeof(IRequest<>))
            .Where(type => new[]
                {
                    type.GetCustomAttribute<AllowAnonymousRequestAttribute>() is not null,
                    type.GetCustomAttribute<RequireRolesAttribute>() is not null,
                    type.GetCustomAttribute<SystemRequestAttribute>() is not null
                }.Count(marked => marked) > 1)
            .Select(type => type.Name);

        contradictory.Should().BeEmpty("a request is open to anonymous callers, restricted to roles or reserved for the system");
    }

    [Fact]
    public void PagedQueries_HaveAPagedQueryValidator()
    {
        var validatedQueries = Layers.Application.GetTypes()
            .Select(type => type.BaseType)
            .OfType<Type>()
            .Where(baseType => baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(PagedQueryValidator<>))
            .Select(baseType => baseType.GetGenericArguments()[0])
            .ToHashSet();

        var unvalidated = Layers.Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IPagedQuery).IsAssignableFrom(type))
            .Where(type => !validatedQueries.Contains(type))
            .Select(type => type.Name);

        unvalidated.Should().BeEmpty("the page number and size come from the client and must be checked");
    }

    private static bool Implements(Type type, Type openGenericInterface) =>
        type.GetInterfaces().Any(implemented =>
            implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openGenericInterface);
}

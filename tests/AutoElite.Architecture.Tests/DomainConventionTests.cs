namespace AutoElite.Architecture.Tests;

public class DomainConventionTests
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly IReadOnlyList<Type> _entities = Layers.ClassesDerivedFrom(Layers.Domain, typeof(BaseEntity));
    private static readonly IReadOnlyList<Type> _valueObjects = Layers.ClassesDerivedFrom(Layers.Domain, typeof(ValueObject));

    [Fact]
    public void DomainAssembly_ContainsEntitiesAndValueObjects()
    {
        // Guards the other tests: an empty type list would let them pass without checking anything.
        _entities.Should().NotBeEmpty();
        _valueObjects.Should().NotBeEmpty();
    }

    [Fact]
    public void EntitiesAndValueObjects_AreSealed()
    {
        var unsealed = _entities.Concat(_valueObjects).Where(type => !type.IsSealed).Select(type => type.Name);

        unsealed.Should().BeEmpty("domain types are not designed for inheritance");
    }

    [Fact]
    public void EntitiesAndValueObjects_HaveNoPublicConstructors()
    {
        var withPublicConstructor = _entities.Concat(_valueObjects)
            .Where(type => type.GetConstructors().Length > 0)
            .Select(type => type.Name);

        withPublicConstructor.Should().BeEmpty("instances are created only by factory methods that validate input");
    }

    [Fact]
    public void Entities_HavePrivateParameterlessConstructor()
    {
        var withoutConstructor = _entities
            .Where(type => type.GetConstructor(InstanceMembers, Type.EmptyTypes) is not { IsPrivate: true })
            .Select(type => type.Name);

        withoutConstructor.Should().BeEmpty("EF Core materializes entities through a private parameterless constructor");
    }

    [Fact]
    public void EntitiesAndValueObjects_HaveNoPublicSetters()
    {
        var withPublicSetter = _entities.Concat(_valueObjects)
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{type.Name}.{property.Name}"));

        withPublicSetter.Should().BeEmpty("state changes only through methods that enforce the rules");
    }

    [Fact]
    public void DomainEvents_AreSealedRecordsNamedDomainEvent()
    {
        var events = Layers.Domain.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(type))
            .ToList();

        // A record is recognized by the clone method the compiler generates for it.
        var violating = events
            .Where(type => !type.IsSealed
                || type.GetMethod("<Clone>$") is null
                || !type.Name.EndsWith("DomainEvent", StringComparison.Ordinal))
            .Select(type => type.Name);

        events.Should().NotBeEmpty();
        violating.Should().BeEmpty("domain events are immutable and named <Something>DomainEvent");
    }
}

using System.Globalization;
using AutoElite.Domain.Enums;

namespace AutoElite.Domain.ValueObjects;

/// <summary>
/// A reference to another object of the system by its kind and identifier, for links from notifications, tasks and
/// documents. Whether the object exists is checked by the application layer.
/// </summary>
public sealed class EntityReference : ValueObject
{
    /// <summary>Gets the kind of the object.</summary>
    public EntityKind Kind { get; }

    /// <summary>Gets the identifier of the object.</summary>
    public Guid Id { get; }

    private EntityReference(EntityKind kind, Guid id)
    {
        Kind = kind;
        Id = id;
    }

    /// <summary>Validates and creates a reference.</summary>
    public static Result<EntityReference> Create(EntityKind kind, Guid id)
    {
        var kindResult = Guard.DefinedEnum(kind, DomainErrors.EntityReference.KindNotDefined);
        if (kindResult.IsFailure)
            return Result.Failure<EntityReference>(kindResult.Error);

        var idResult = Guard.NotEmpty(id, DomainErrors.EntityReference.IdEmpty);
        if (idResult.IsFailure)
            return Result.Failure<EntityReference>(idResult.Error);

        return new EntityReference(kind, id);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return Id;
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Kind}/{Id}");
}

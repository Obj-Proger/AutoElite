using AutoElite.Domain.Common;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.Outbox;

namespace AutoElite.Infrastructure.Tests.Outbox;

public sealed class OutboxSerializerTests
{
    private static readonly DateTime _occurredOnUtc = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<Type, object> _samples = new()
    {
        [typeof(Guid)] = Guid.CreateVersion7(),
        [typeof(DateTime)] = _occurredOnUtc,
        [typeof(string)] = "Текст",
        [typeof(bool)] = true,
        [typeof(int)] = 7,
        [typeof(decimal)] = 1234.56m
    };

    public static TheoryData<string> EventTypeNames() => [.. OutboxSerializer.EventTypes.Select(type => type.Name).Order()];

    [Theory]
    [MemberData(nameof(EventTypeNames))]
    public void Deserialize_SerializedEvent_ReturnsAnEqualEvent(string typeName)
    {
        var domainEvent = SampleOf(OutboxSerializer.EventTypes.Single(type => type.Name == typeName));

        var restored = OutboxSerializer.Deserialize(OutboxSerializer.TypeNameOf(domainEvent), OutboxSerializer.Serialize(domainEvent));

        restored.Should().Be(domainEvent);
        restored.OccurredOnUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Serialize_Enumeration_WritesItsName()
    {
        var domainEvent = new NotificationCreatedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), NotificationType.ExamReminder, _occurredOnUtc);

        OutboxSerializer.Serialize(domainEvent).Should().Contain("\"ExamReminder\"");
    }

    [Fact]
    public void Deserialize_UnknownType_Throws()
    {
        var deserialize = () => OutboxSerializer.Deserialize("NoSuchDomainEvent", "{}");

        deserialize.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TypeNameOf_EventFromOutsideTheDomain_Throws()
    {
        var typeNameOf = () => OutboxSerializer.TypeNameOf(new ForeignDomainEvent(_occurredOnUtc));

        typeNameOf.Should().Throw<ArgumentException>();
    }

    /// <summary>Builds an event of the type with a sample value for every parameter of its constructor.</summary>
    private static IDomainEvent SampleOf(Type eventType)
    {
        var constructor = eventType.GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(parameter => SampleValue(parameter.ParameterType)).ToArray();

        return (IDomainEvent)constructor.Invoke(arguments);
    }

    private static object SampleValue(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
        {
            var values = Enum.GetValues(underlying);

            return values.GetValue(values.Length - 1)!;
        }

        return _samples.TryGetValue(underlying, out var sample)
            ? sample
            : throw new NotSupportedException($"Add a sample value of {type.Name} for the outbox serializer tests.");
    }

    private sealed record ForeignDomainEvent(DateTime OccurredOnUtc) : IDomainEvent;
}

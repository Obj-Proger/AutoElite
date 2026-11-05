using AutoElite.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Mappings that the configurations of all aggregates share.</summary>
internal static class EntityTypeBuilderExtensions
{
    /// <summary>The longest IANA time zone identifier a column holds; real identifiers are about half as long.</summary>
    public const int TimeZoneIdMaxLength = 64;

    /// <summary>The name of the shadow property that holds the row version of an aggregate.</summary>
    public const string RowVersion = "RowVersion";

    /// <summary>
    /// Maps the key, which the domain generates itself, and the row version of an aggregate root: PostgreSQL's
    /// <c>xmin</c> system column, which changes with every update of the row, so that a save based on a stale copy
    /// fails instead of overwriting another request's change.
    /// </summary>
    /// <param name="builder">The builder of the aggregate root.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    public static void ConfigureAggregateRoot<TAggregate>(this EntityTypeBuilder<TAggregate> builder, string tableName)
        where TAggregate : BaseEntity
    {
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasRowVersion();
        builder.Ignore(entity => entity.DomainEvents);
    }

    /// <summary>
    /// Maps PostgreSQL's <c>xmin</c> system column as the row version of the entity, a shadow property that changes with
    /// every update, so that saving a stale copy fails.
    /// </summary>
    /// <param name="builder">The builder of the entity.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public static void HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class =>
        builder.Property<uint>(RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

    /// <summary>
    /// Maps a child entity of an aggregate: the key the domain generates and the foreign key to the root, named
    /// <paramref name="rootKey"/>, which is a shadow property because the child does not know its root.
    /// </summary>
    /// <param name="builder">The builder of the child entity.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="rootKey">The name of the foreign key to the root, such as <c>LeadId</c>.</param>
    /// <typeparam name="TChild">The type of the child entity.</typeparam>
    public static void ConfigureChild<TChild>(this EntityTypeBuilder<TChild> builder, string tableName, string rootKey)
        where TChild : BaseEntity
    {
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property<Guid>(rootKey);
        builder.Ignore(entity => entity.DomainEvents);
    }

    /// <summary>
    /// Maps <see cref="Money"/> to the columns <c>{prefix}_amount</c>, with the precision of
    /// <see cref="Money.MaxAbsoluteAmount"/> and two decimals, and <c>{prefix}_currency</c>.
    /// </summary>
    /// <param name="money">The builder of the complex property.</param>
    /// <remarks>
    /// The currency is mapped explicitly as a converted column: left to the conventions, a value object inside a
    /// complex type is not taken as a column, and the constructor of <see cref="Money"/> could not be bound.
    /// </remarks>
    public static void MapMoney(this ComplexPropertyBuilder<Money> money)
    {
        money.Property(value => value.Amount).HasPrecision(MoneyPrecision, MoneyScale);
        money.Property(value => value.Currency).HasConversion<CurrencyConverter>().HasMaxLength(Currency.CodeLength);
    }

    /// <summary>The digits of an amount: enough for <see cref="Money.MaxAbsoluteAmount"/>.</summary>
    public const int MoneyPrecision = 14;

    /// <summary>The decimals of an amount; currencies without minor units store zeros there.</summary>
    public const int MoneyScale = 2;

    /// <summary>
    /// Maps a <see cref="TimeInterval"/> to the columns <c>start_utc</c> and <c>end_utc</c>, the names the exclusion
    /// constraints and indexes use.
    /// </summary>
    /// <param name="time">The builder of the complex property.</param>
    public static void MapTime(this ComplexPropertyBuilder<TimeInterval> time)
    {
        time.Property(value => value.StartUtc).HasColumnName("start_utc");
        time.Property(value => value.EndUtc).HasColumnName("end_utc");
    }

    /// <summary>Maps an <see cref="Address"/> to the columns <c>{prefix}_city</c> and <c>{prefix}_street</c>.</summary>
    /// <param name="address">The builder of the complex property.</param>
    public static void MapAddress(this ComplexPropertyBuilder<Address> address)
    {
        address.Property(value => value.City).HasMaxLength(Address.MaxCityLength);
        address.Property(value => value.Street).HasMaxLength(Address.MaxStreetLength);
    }
}

using AutoElite.Infrastructure.Persistence.Conversions;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// The database of the school. Each aggregate is mapped by its own configuration in <c>Persistence/Configurations</c>;
/// repositories and read services reach the tables through <see cref="DbContext.Set{TEntity}()"/>.
/// </summary>
/// <remarks>
/// Names are snake_case (<see cref="PersistenceOptions.UseAutoElite{TBuilder}"/>). Enumerations are stored by name, so
/// that the data stays readable and survives reordering of the members. Value objects with a single value are stored
/// as that value; the others are complex properties of their owners.
/// </remarks>
public sealed class AutoEliteDbContext(DbContextOptions<AutoEliteDbContext> options) : DbContext(options)
{
    /// <summary>The longest name of an enumeration member that a column holds.</summary>
    internal const int EnumMaxLength = 64;

    /// <summary>
    /// The collation that compares text ignoring case, for columns whose values are unique regardless of case, such as
    /// a branch name. It is nondeterministic, so equality and unique indexes on such a column ignore case.
    /// </summary>
    internal const string CaseInsensitiveCollation = "case_insensitive";

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        foreach (var enumType in typeof(BaseEntity).Assembly.GetTypes().Where(type => type is { IsEnum: true, IsPublic: true }))
            configurationBuilder.Properties(enumType).HaveConversion<string>().HaveMaxLength(EnumMaxLength);

        configurationBuilder.Properties<Currency>().HaveConversion<CurrencyConverter>().HaveMaxLength(Currency.CodeLength);
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>().HaveMaxLength(PhoneNumber.MaxLength);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoEliteDbContext).Assembly);
    }
}

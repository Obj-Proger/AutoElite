using AutoElite.Infrastructure.Identity;
using AutoElite.Infrastructure.Persistence.Conversions;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// The database of the school. Each aggregate is mapped by its own configuration in <c>Persistence/Configurations</c>;
/// repositories and read services reach the tables through <see cref="DbContext.Set{TEntity}()"/>.
/// </summary>
/// <remarks>
/// Names are snake_case (<see cref="PersistenceOptions.UseAutoElite{TBuilder}"/>). Enumerations are stored by name, so
/// that the data stays readable and survives reordering of the members. Value objects with a single value are stored
/// as that value; the others are complex properties of their owners.
/// <para>
/// The context also holds the sign-in accounts of ASP.NET Core Identity, so that a user and the account are saved in
/// one transaction, and the data protection keys that protect the tokens sent by email, so that the tokens stay valid
/// across restarts and instances.
/// </para>
/// </remarks>
public sealed class AutoEliteDbContext(DbContextOptions<AutoEliteDbContext> options)
    : IdentityUserContext<UserAccount, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>The longest name of an enumeration member that a column holds.</summary>
    internal const int EnumMaxLength = 64;

    /// <summary>
    /// The collation that compares text ignoring case, for columns whose values are unique regardless of case, such as
    /// a branch name. It is nondeterministic, so equality and unique indexes on such a column ignore case.
    /// </summary>
    internal const string CaseInsensitiveCollation = "case_insensitive";

    /// <inheritdoc />
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        foreach (var enumType in typeof(BaseEntity).Assembly.GetTypes().Where(type => type is { IsEnum: true, IsPublic: true }))
            configurationBuilder.Properties(enumType).HaveConversion<string>().HaveMaxLength(EnumMaxLength);

        configurationBuilder.Properties<Currency>().HaveConversion<CurrencyConverter>().HaveMaxLength(Currency.CodeLength);
        configurationBuilder.Properties<Email>().HaveConversion<EmailConverter>().HaveMaxLength(Email.MaxLength);
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>().HaveMaxLength(PhoneNumber.MaxLength);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_account_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_account_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_account_tokens");
        builder.Entity<DataProtectionKey>().ToTable("data_protection_keys");

        // Passkeys are mapped only by the newer identity schema versions; renaming must not add them to the model.
        if (builder.Model.FindEntityType(typeof(IdentityUserPasskey<Guid>)) is not null)
            builder.Entity<IdentityUserPasskey<Guid>>().ToTable("user_account_passkeys");

        builder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        builder.ApplyConfigurationsFromAssembly(typeof(AutoEliteDbContext).Assembly);
    }
}

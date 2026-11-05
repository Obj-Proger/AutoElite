using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Branch"/>; the name is unique regardless of case.</summary>
internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ConfigureAggregateRoot("branches");

        builder.Property(branch => branch.Name)
            .HasMaxLength(Branch.MaxNameLength)
            .UseCollation(AutoEliteDbContext.CaseInsensitiveCollation);
        builder.HasIndex(branch => branch.Name).IsUnique();

        builder.ComplexProperty(branch => branch.Address, address => address.MapAddress());
        builder.Property(branch => branch.TimeZoneId).HasMaxLength(EntityTypeBuilderExtensions.TimeZoneIdMaxLength);
    }
}

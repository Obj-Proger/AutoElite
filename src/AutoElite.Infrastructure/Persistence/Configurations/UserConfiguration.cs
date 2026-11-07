using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="User"/>; the email address, stored in lower case, is unique.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureAggregateRoot("users");

        builder.ComplexProperty(user => user.FullName, name =>
        {
            name.Property(value => value.LastName).HasMaxLength(FullName.MaxNamePartLength);
            name.Property(value => value.FirstName).HasMaxLength(FullName.MaxNamePartLength);
            name.Property(value => value.MiddleName).HasMaxLength(FullName.MaxNamePartLength);
        });
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasOne<Branch>().WithMany().HasForeignKey(user => user.BranchId).OnDelete(DeleteBehavior.Restrict);

        // Removing the photo empties the reference; the domain handler of the deletion then finds nothing to change.
        builder.HasOne<Document>().WithMany().HasForeignKey(user => user.PhotoDocumentId).OnDelete(DeleteBehavior.SetNull);
    }
}

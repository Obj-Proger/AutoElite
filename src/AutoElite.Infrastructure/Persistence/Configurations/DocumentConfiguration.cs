using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Document"/>. The owner is a required complex property of two columns, indexed by a script
/// (<c>05-document-owner</c>); every file has a storage key of its own.
/// </summary>
internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    /// <summary>The longest content type; the allowed ones are much shorter.</summary>
    private const int MaxContentTypeLength = 128;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ConfigureAggregateRoot("documents");

        builder.ComplexProperty(document => document.Owner, owner =>
        {
            owner.Property(reference => reference.Kind).HasColumnName("owner_kind");
            owner.Property(reference => reference.Id).HasColumnName("owner_id");
        });
        builder.Property(document => document.FileName).HasMaxLength(Document.MaxFileNameLength);
        builder.Property(document => document.ContentType).HasMaxLength(MaxContentTypeLength);
        builder.Property(document => document.StorageKey).HasMaxLength(Document.MaxStorageKeyLength);
        builder.HasIndex(document => document.StorageKey).IsUnique();
        builder.Property(document => document.Description).HasMaxLength(Document.MaxDescriptionLength);
        builder.HasOne<User>().WithMany().HasForeignKey(document => document.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

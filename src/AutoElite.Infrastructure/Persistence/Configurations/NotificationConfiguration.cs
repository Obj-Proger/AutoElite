using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Notification"/>. The record a notification is about is an optional complex property of two
/// columns, both empty when there is none; its index is a script (<c>03-notification-subject</c>), since the columns of
/// a complex property cannot be indexed in the model.
/// </summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ConfigureAggregateRoot("notifications");

        builder.Property(notification => notification.Title).HasMaxLength(Notification.MaxTitleLength);
        builder.Property(notification => notification.Body).HasMaxLength(Notification.MaxBodyLength);
        builder.ComplexProperty(notification => notification.Subject, subject =>
        {
            subject.Property(reference => reference.Kind).HasColumnName("subject_kind");
            subject.Property(reference => reference.Id).HasColumnName("subject_id");
        });
        builder.HasOne<User>().WithMany().HasForeignKey(notification => notification.RecipientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(notification => new { notification.RecipientId, notification.CreatedAtUtc });
        builder.HasIndex(notification => notification.RecipientId)
            .HasFilter("read_at_utc IS NULL")
            .HasDatabaseName("ix_notifications_recipient_id_unread");
    }
}

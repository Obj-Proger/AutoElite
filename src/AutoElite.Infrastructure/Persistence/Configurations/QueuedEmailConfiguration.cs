using AutoElite.Infrastructure.Mail;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="QueuedEmail"/>; the delivery job finds waiting emails through a partial index.</summary>
internal sealed class QueuedEmailConfiguration : IEntityTypeConfiguration<QueuedEmail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<QueuedEmail> builder)
    {
        builder.ToTable("queued_emails");
        builder.HasKey(email => email.Id);
        builder.Property(email => email.Id).ValueGeneratedNever();
        builder.Property(email => email.To).HasColumnName("to_address");
        builder.Property(email => email.Subject).HasMaxLength(QueuedEmail.MaxSubjectLength);
        builder.Property(email => email.LastError).HasMaxLength(QueuedEmail.MaxErrorLength);
        builder.HasRowVersion();
        builder.HasIndex(email => email.NextAttemptAtUtc).HasFilter("sent_at_utc IS NULL");
    }
}

using AutoElite.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="OutboxMessage"/>; the processor finds waiting messages through a partial index.</summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(OutboxMessage.MaxTypeLength);
        builder.Property(message => message.Payload).HasColumnType("jsonb");
        builder.Property(message => message.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);
        builder.HasIndex(message => message.NextAttemptOnUtc).HasFilter("processed_on_utc IS NULL");
    }
}

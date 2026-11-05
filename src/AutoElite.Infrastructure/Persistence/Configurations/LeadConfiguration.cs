using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Lead"/> with its notes and status history, which are loaded and saved with the lead.</summary>
internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    /// <summary>The foreign key of the children to the lead.</summary>
    public const string LeadKey = "LeadId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ConfigureAggregateRoot("leads");

        builder.Property(lead => lead.ContactName).HasMaxLength(Lead.MaxContactNameLength);
        builder.Property(lead => lead.Comment).HasMaxLength(Lead.MaxCommentLength);
        builder.Property(lead => lead.LostReason).HasMaxLength(Lead.MaxLostReasonLength);
        builder.HasOne<Branch>().WithMany().HasForeignKey(lead => lead.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(lead => lead.ManagerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(lead => new { lead.Status, lead.CreatedAtUtc });
        builder.HasIndex(lead => lead.CreatedAtUtc);

        builder.HasMany(lead => lead.Notes).WithOne().HasForeignKey(LeadKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lead => lead.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(lead => lead.StatusHistory).WithOne().HasForeignKey(LeadKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lead => lead.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps <see cref="LeadNote"/>, a note of a lead's journal.</summary>
internal sealed class LeadNoteConfiguration : IEntityTypeConfiguration<LeadNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeadNote> builder)
    {
        builder.ConfigureChild("lead_notes", LeadConfiguration.LeadKey);
        builder.Property(note => note.Text).HasMaxLength(LeadNote.MaxTextLength);
        builder.HasOne<User>().WithMany().HasForeignKey(note => note.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Maps <see cref="LeadStatusChange"/>, a transition of a lead's funnel.</summary>
internal sealed class LeadStatusChangeConfiguration : IEntityTypeConfiguration<LeadStatusChange>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeadStatusChange> builder)
    {
        builder.ConfigureChild("lead_status_changes", LeadConfiguration.LeadKey);
        builder.Property(change => change.Reason).HasMaxLength(Lead.MaxLostReasonLength);
        builder.HasOne<User>().WithMany().HasForeignKey(change => change.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

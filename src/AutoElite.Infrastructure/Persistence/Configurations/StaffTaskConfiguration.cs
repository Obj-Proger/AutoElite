using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="StaffTask"/>. The record a task is about is an optional complex property of two columns, indexed
/// by a script (<c>04-staff-task-subject</c>); the overdue check finds open tasks through a partial index.
/// </summary>
internal sealed class StaffTaskConfiguration : IEntityTypeConfiguration<StaffTask>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffTask> builder)
    {
        builder.ConfigureAggregateRoot("staff_tasks");

        builder.Property(task => task.Title).HasMaxLength(StaffTask.MaxTitleLength);
        builder.Property(task => task.Description).HasMaxLength(StaffTask.MaxDescriptionLength);
        builder.ComplexProperty(task => task.Subject, subject =>
        {
            subject.Property(reference => reference.Kind).HasColumnName("subject_kind");
            subject.Property(reference => reference.Id).HasColumnName("subject_id");
        });
        builder.HasOne<User>().WithMany().HasForeignKey(task => task.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(task => new { task.AssigneeId, task.DueAtUtc });
        builder.HasIndex(task => task.DueAtUtc)
            .HasFilter("status <> 'Done'")
            .HasDatabaseName("ix_staff_tasks_due_at_utc_open");
    }
}

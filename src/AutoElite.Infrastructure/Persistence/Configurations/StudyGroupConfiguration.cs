using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="StudyGroup"/> with its enrollments, which are loaded and saved with the group.</summary>
internal sealed class StudyGroupConfiguration : IEntityTypeConfiguration<StudyGroup>
{
    /// <summary>The foreign key of the enrollments to the group.</summary>
    public const string GroupKey = "StudyGroupId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyGroup> builder)
    {
        builder.ConfigureAggregateRoot("study_groups");

        builder.Property(group => group.Name).HasMaxLength(StudyGroup.MaxNameLength);
        builder.HasOne<Course>().WithMany().HasForeignKey(group => group.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(group => group.TheoryInstructorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(group => group.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(group => group.StartDate);

        builder.HasMany(group => group.Enrollments).WithOne().HasForeignKey(GroupKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(group => group.Enrollments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// Maps <see cref="GroupEnrollment"/>; a contract is a current member of a group at most once, which a partial unique
/// index makes sure of even when two requests enroll it at the same time.
/// </summary>
internal sealed class GroupEnrollmentConfiguration : IEntityTypeConfiguration<GroupEnrollment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GroupEnrollment> builder)
    {
        builder.ConfigureChild("group_enrollments", StudyGroupConfiguration.GroupKey);
        builder.HasOne<Contract>().WithMany().HasForeignKey(enrollment => enrollment.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(enrollment => enrollment.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(StudyGroupConfiguration.GroupKey, nameof(GroupEnrollment.ContractId))
            .IsUnique()
            .HasFilter("withdrawn_at_utc IS NULL");
        builder.HasIndex(enrollment => enrollment.ContractId);
    }
}

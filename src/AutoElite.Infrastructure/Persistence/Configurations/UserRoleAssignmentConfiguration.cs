using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="UserRoleAssignment"/>; a user holds each role at most once.</summary>
internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ConfigureAggregateRoot("user_role_assignments");

        builder.HasIndex(assignment => new { assignment.UserId, assignment.Role }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(assignment => assignment.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

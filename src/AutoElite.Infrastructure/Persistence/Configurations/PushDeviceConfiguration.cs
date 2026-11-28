using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="PushDevice"/>: the token is unique, since it belongs to one installation, and the devices of a user
/// are found by the index on the user. The name of the foreign key is given, so that it does not depend on the order in
/// which the configurations name the tables; a user's devices go with the user.
/// </summary>
internal sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.ConfigureAggregateRoot("push_devices");

        builder.Property(device => device.Token).HasMaxLength(PushDevice.MaxTokenLength);
        builder.HasIndex(device => device.Token).IsUnique();
        builder.HasOne<User>().WithMany()
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_push_devices_users_user_id");
    }
}

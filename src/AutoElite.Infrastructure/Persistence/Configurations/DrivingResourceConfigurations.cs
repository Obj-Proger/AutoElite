using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Vehicle"/>; the plate, stored normalized, is unique.</summary>
internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ConfigureAggregateRoot("vehicles");

        builder.HasIndex(vehicle => vehicle.Plate).IsUnique();
        builder.Property(vehicle => vehicle.Make).HasMaxLength(Vehicle.MaxMakeLength);
        builder.Property(vehicle => vehicle.Model).HasMaxLength(Vehicle.MaxModelLength);
        builder.HasOne<Branch>().WithMany().HasForeignKey(vehicle => vehicle.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Maps <see cref="TrainingGround"/>.</summary>
internal sealed class TrainingGroundConfiguration : IEntityTypeConfiguration<TrainingGround>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrainingGround> builder)
    {
        builder.ConfigureAggregateRoot("training_grounds");

        builder.Property(ground => ground.Name).HasMaxLength(TrainingGround.MaxNameLength);
        builder.Property(ground => ground.Description).HasMaxLength(TrainingGround.MaxDescriptionLength);
        builder.ComplexProperty(ground => ground.Address, address => address.MapAddress());
        builder.HasOne<Branch>().WithMany().HasForeignKey(ground => ground.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Maps <see cref="DrivingRoute"/>.</summary>
internal sealed class DrivingRouteConfiguration : IEntityTypeConfiguration<DrivingRoute>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DrivingRoute> builder)
    {
        builder.ConfigureAggregateRoot("driving_routes");

        builder.Property(route => route.Name).HasMaxLength(DrivingRoute.MaxNameLength);
        builder.Property(route => route.Description).HasMaxLength(DrivingRoute.MaxDescriptionLength);
        builder.HasOne<User>().WithMany().HasForeignKey(route => route.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TrainingGround>().WithMany()
            .HasForeignKey(route => route.TrainingGroundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

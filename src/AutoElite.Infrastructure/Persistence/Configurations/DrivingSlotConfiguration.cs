using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="DrivingSlot"/> with its bookings. Overlapping slots of an instructor or a vehicle, and overlapping
/// active bookings of a student, are rejected by exclusion constraints (<see cref="DatabaseObjects"/>).
/// </summary>
internal sealed class DrivingSlotConfiguration : IEntityTypeConfiguration<DrivingSlot>
{
    /// <summary>The foreign key of the bookings to the slot; the booking trigger reads it as <c>driving_slot_id</c>.</summary>
    public const string SlotKey = "DrivingSlotId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DrivingSlot> builder)
    {
        builder.ConfigureAggregateRoot("driving_slots");

        builder.ComplexProperty(slot => slot.Time, time => time.MapTime());
        builder.HasOne<User>().WithMany().HasForeignKey(slot => slot.InstructorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(slot => slot.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TrainingGround>().WithMany()
            .HasForeignKey(slot => slot.TrainingGroundId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DrivingRoute>().WithMany().HasForeignKey(slot => slot.RouteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(slot => slot.Status);

        builder.HasMany(slot => slot.Bookings).WithOne().HasForeignKey(SlotKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(slot => slot.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps <see cref="DrivingBooking"/>; the grades of its skills are a JSON column of the booking.</summary>
internal sealed class DrivingBookingConfiguration : IEntityTypeConfiguration<DrivingBooking>
{
    /// <summary>The digits of the actual hours of a lesson, up to <see cref="DrivingSlot.MaxDuration"/>.</summary>
    private const int HoursPrecision = 4;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DrivingBooking> builder)
    {
        builder.ConfigureChild("driving_bookings", DrivingSlotConfiguration.SlotKey);

        builder.Property(booking => booking.CancellationReason).HasMaxLength(DrivingSlot.MaxReasonLength);
        builder.Property(booking => booking.InstructorNote).HasMaxLength(DrivingSlot.MaxNoteLength);
        builder.Property(booking => booking.StudentComment).HasMaxLength(DrivingSlot.MaxNoteLength);
        builder.Property(booking => booking.ActualHours).HasPrecision(HoursPrecision, Contract.GradeDecimals);
        builder.OwnsMany(booking => booking.SkillGrades, grades =>
        {
            grades.ToJson();

            // Get-only properties of a value object are not mapped by convention; the constructor needs them.
            grades.Property(grade => grade.Skill);
            grades.Property(grade => grade.Grade);
        });
        builder.Navigation(booking => booking.SkillGrades).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Contract>().WithMany().HasForeignKey(booking => booking.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(booking => booking.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany()
            .HasForeignKey(booking => booking.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

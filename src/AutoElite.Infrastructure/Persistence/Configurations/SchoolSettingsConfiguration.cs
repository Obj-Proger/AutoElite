using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="SchoolSettings"/>. The school has one settings record: every row has the shadow column
/// <see cref="Singleton"/> set to true, and a unique index on it rejects a second row, so two requests that create
/// the missing record at the same time cannot both succeed.
/// </summary>
internal sealed class SchoolSettingsConfiguration : IEntityTypeConfiguration<SchoolSettings>
{
    /// <summary>The name of the shadow property that makes the record single.</summary>
    public const string Singleton = "Singleton";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SchoolSettings> builder)
    {
        builder.ConfigureAggregateRoot("school_settings");

        builder.Property<bool>(Singleton).ValueGeneratedNever();
        builder.HasIndex(Singleton).IsUnique();
        builder.ToTable("school_settings", table => table.HasCheckConstraint("ck_school_settings_singleton", "singleton"));

        builder.Property(settings => settings.TimeZoneId).HasMaxLength(EntityTypeBuilderExtensions.TimeZoneIdMaxLength);
        builder.PrimitiveCollection(settings => settings.LessonReminderLeadTimes)
            .HasField("_lessonReminderLeadTimes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

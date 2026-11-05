using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TheoryLesson"/> with its attendance marks. Overlapping lessons of a group or an instructor are
/// rejected by exclusion constraints (<see cref="DatabaseObjects"/>), which need the time as two plain columns.
/// </summary>
internal sealed class TheoryLessonConfiguration : IEntityTypeConfiguration<TheoryLesson>
{
    /// <summary>The foreign key of the marks to the lesson.</summary>
    public const string LessonKey = "TheoryLessonId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TheoryLesson> builder)
    {
        builder.ConfigureAggregateRoot("theory_lessons");

        builder.ComplexProperty(lesson => lesson.Time, time => time.MapTime());
        builder.Property(lesson => lesson.CancellationReason).HasMaxLength(TheoryLesson.MaxCancellationReasonLength);
        builder.HasOne<StudyGroup>().WithMany().HasForeignKey(lesson => lesson.GroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CourseTopic>().WithMany().HasForeignKey(lesson => lesson.TopicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(lesson => lesson.InstructorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(lesson => lesson.Status);

        builder.HasMany(lesson => lesson.Attendance).WithOne().HasForeignKey(LessonKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lesson => lesson.Attendance).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps <see cref="LessonAttendance"/>; a contract has at most one mark per lesson.</summary>
internal sealed class LessonAttendanceConfiguration : IEntityTypeConfiguration<LessonAttendance>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LessonAttendance> builder)
    {
        builder.ConfigureChild("lesson_attendance", TheoryLessonConfiguration.LessonKey);
        builder.Property(mark => mark.Comment).HasMaxLength(LessonAttendance.MaxCommentLength);
        builder.HasIndex(TheoryLessonConfiguration.LessonKey, nameof(LessonAttendance.ContractId)).IsUnique();
        builder.HasIndex(mark => mark.StudentId);
        builder.HasOne<Contract>().WithMany().HasForeignKey(mark => mark.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(mark => mark.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(mark => mark.MarkedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

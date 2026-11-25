using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="PracticeRecord"/>: one row per student and question, which the unique index keeps so, and which also
/// serves the foreign key to the student. The names of the foreign keys are given, so that they do not depend on the
/// order in which the configurations name the tables.
/// </summary>
internal sealed class PracticeRecordConfiguration : IEntityTypeConfiguration<PracticeRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PracticeRecord> builder)
    {
        builder.ConfigureAggregateRoot("practice_records");

        builder.HasOne<User>().WithMany()
            .HasForeignKey(record => record.StudentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_practice_records_users_student_id");
        builder.HasOne<Question>().WithMany()
            .HasForeignKey(record => record.QuestionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_practice_records_questions_question_id");
        builder.HasIndex(record => new { record.StudentId, record.QuestionId }).IsUnique();
    }
}

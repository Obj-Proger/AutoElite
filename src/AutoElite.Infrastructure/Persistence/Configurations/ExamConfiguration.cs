using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Exam"/> with its results.</summary>
internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    /// <summary>The foreign key of the results to the exam.</summary>
    public const string ExamKey = "ExamId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ConfigureAggregateRoot("exams");

        builder.Property(exam => exam.CancellationReason).HasMaxLength(Exam.MaxCancellationReasonLength);
        builder.HasOne<StudyGroup>().WithMany().HasForeignKey(exam => exam.GroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TestTemplate>().WithMany().HasForeignKey(exam => exam.TestTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(exam => exam.ScheduledAtUtc);

        builder.HasMany(exam => exam.Results).WithOne().HasForeignKey(ExamKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(exam => exam.Results).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// Maps <see cref="ExamResult"/>; a contract has at most one result per exam, which a unique index makes sure of when
/// two results are recorded at once.
/// </summary>
internal sealed class ExamResultConfiguration : IEntityTypeConfiguration<ExamResult>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExamResult> builder)
    {
        builder.ConfigureChild("exam_results", ExamConfiguration.ExamKey);

        builder.Property(result => result.Comment).HasMaxLength(Exam.MaxCommentLength);
        builder.HasIndex(ExamConfiguration.ExamKey, nameof(ExamResult.ContractId)).IsUnique();
        builder.HasIndex(result => result.StudentId);
        builder.HasOne<Contract>().WithMany().HasForeignKey(result => result.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(result => result.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TestSession>().WithMany().HasForeignKey(result => result.TestSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(result => result.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(result => result.CorrectedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

using AutoElite.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TestSession"/> with its questions. The rules and blocks are copies of the template's taken at the
/// start, stored like the template's; a contract has at most one attempt in progress, which a partial unique index
/// makes sure of when two attempts are started at once.
/// </summary>
internal sealed class TestSessionConfiguration : IEntityTypeConfiguration<TestSession>
{
    /// <summary>The foreign key of the questions to the attempt.</summary>
    public const string SessionKey = "TestSessionId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestSession> builder)
    {
        builder.ConfigureAggregateRoot("test_sessions");

        builder.ComplexProperty(session => session.Rules, rules => rules.MapRules());
        builder.Ignore(session => session.Blocks);
        builder.Property<List<TestBlock>>(TestTemplateConfiguration.BlocksField)
            .HasColumnName("blocks")
            .HasColumnType("jsonb")
            .HasConversion(new TestBlocksConverter(), TestBlocksConverter.Comparer);
        builder.PrimitiveCollection(session => session.PendingAddOnBlocks)
            .HasField("_pendingAddOnBlocks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Contract>().WithMany().HasForeignKey(session => session.ContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(session => session.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TestTemplate>().WithMany().HasForeignKey(session => session.TemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Exam>().WithMany().HasForeignKey(session => session.ExamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(session => session.ContractId)
            .IsUnique()
            .HasFilter("status = 'InProgress'")
            .HasDatabaseName("ux_test_sessions_contract_in_progress");
        builder.HasIndex(session => new { session.ExamId, session.ContractId });

        builder.HasMany(session => session.Questions).WithOne().HasForeignKey(SessionKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(session => session.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// Maps <see cref="SessionQuestion"/>, a copy of a bank question as shown in an attempt; its answer options are a JSON
/// column, copies as well.
/// </summary>
internal sealed class SessionQuestionConfiguration : IEntityTypeConfiguration<SessionQuestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessionQuestion> builder)
    {
        builder.ConfigureChild("session_questions", TestSessionConfiguration.SessionKey);

        builder.Property(question => question.Text).HasMaxLength(Question.MaxTextLength);
        builder.Property(question => question.Explanation).HasMaxLength(Question.MaxExplanationLength);
        builder.HasOne<Question>().WithMany().HasForeignKey(question => question.QuestionId).OnDelete(DeleteBehavior.Restrict);

        // An attempt keeps its questions when their image is removed, without the image.
        builder.HasOne<Document>().WithMany().HasForeignKey(question => question.ImageDocumentId).OnDelete(DeleteBehavior.SetNull);
        builder.OwnsMany(question => question.Options, options =>
        {
            options.ToJson();

            // Get-only properties of a value object are not mapped by convention; the constructor needs them.
            options.Property(option => option.OptionId);
            options.Property(option => option.Text);
        });
        builder.Navigation(question => question.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

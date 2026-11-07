using AutoElite.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Question"/> with its answer options; the licence categories are an array column, and the external
/// code is unique regardless of case, as the import matches it.
/// </summary>
internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    /// <summary>The foreign key of the options to the question.</summary>
    public const string QuestionKey = "QuestionId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ConfigureAggregateRoot("questions");

        builder.Property(question => question.Text).HasMaxLength(Question.MaxTextLength);
        builder.Property(question => question.Explanation).HasMaxLength(Question.MaxExplanationLength);
        builder.Property(question => question.ExternalCode)
            .HasMaxLength(Question.MaxExternalCodeLength)
            .UseCollation(AutoEliteDbContext.CaseInsensitiveCollation);
        builder.HasIndex(question => question.ExternalCode).IsUnique().HasFilter("external_code IS NOT NULL");
        builder.PrimitiveCollection(question => question.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<CourseTopic>().WithMany().HasForeignKey(question => question.TopicId).OnDelete(DeleteBehavior.Restrict);

        // Removing the image empties the reference; the domain handler of the deletion then finds nothing to change.
        builder.HasOne<Document>().WithMany().HasForeignKey(question => question.ImageDocumentId).OnDelete(DeleteBehavior.SetNull);
        builder.Ignore(question => question.CorrectOption);

        builder.HasMany(question => question.Options).WithOne().HasForeignKey(QuestionKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(question => question.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps <see cref="AnswerOption"/>, an option of a question.</summary>
internal sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ConfigureChild("answer_options", QuestionConfiguration.QuestionKey);
        builder.Property(option => option.Text).HasMaxLength(Question.MaxOptionTextLength);
    }
}

/// <summary>
/// Maps <see cref="TestTemplate"/>: the rules are columns of the template, and the blocks a JSON column
/// (<see cref="TestBlocksConverter"/>).
/// </summary>
internal sealed class TestTemplateConfiguration : IEntityTypeConfiguration<TestTemplate>
{
    /// <summary>The name of the field that holds the blocks, mapped in place of the read-only view of them.</summary>
    public const string BlocksField = "_blocks";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestTemplate> builder)
    {
        builder.ConfigureAggregateRoot("test_templates");

        builder.Property(template => template.Name).HasMaxLength(TestTemplate.MaxNameLength);
        builder.ComplexProperty(template => template.Rules, rules => rules.MapRules());
        builder.Ignore(template => template.Blocks);
        builder.Property<List<TestBlock>>(BlocksField)
            .HasColumnName("blocks")
            .HasColumnType("jsonb")
            .HasConversion(new TestBlocksConverter(), TestBlocksConverter.Comparer);
    }
}

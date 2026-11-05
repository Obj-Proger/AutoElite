using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Course"/> with its curriculum; topics removed from the course are deleted.</summary>
internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    /// <summary>The foreign key of the topics to the course.</summary>
    public const string CourseKey = "CourseId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ConfigureAggregateRoot("courses");

        builder.Property(course => course.Name).HasMaxLength(Course.MaxNameLength);
        builder.Property(course => course.Description).HasMaxLength(Course.MaxDescriptionLength);
        builder.ComplexProperty(course => course.Price, price => price.MapMoney());

        builder.HasMany(course => course.Topics).WithOne().HasForeignKey(CourseKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(course => course.Topics).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps <see cref="CourseTopic"/>, a topic of a course's curriculum.</summary>
internal sealed class CourseTopicConfiguration : IEntityTypeConfiguration<CourseTopic>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CourseTopic> builder)
    {
        builder.ConfigureChild("course_topics", CourseConfiguration.CourseKey);
        builder.Property(topic => topic.Title).HasMaxLength(CourseTopic.MaxTitleLength);
        builder.Property(topic => topic.Description).HasMaxLength(CourseTopic.MaxDescriptionLength);
    }
}

namespace AutoElite.Application.Courses.CreateCourse;

/// <summary>Creates the course with its price in the given or the default currency.</summary>
internal sealed class CreateCourseCommandHandler(ICourseRepository courseRepository, CoursePrice coursePrice)
    : ICommandHandler<CreateCourseCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var priceResult = await coursePrice.CreateAsync(command.PriceAmount, command.PriceCurrency, cancellationToken);
        if (priceResult.IsFailure)
            return Result.Failure<Guid>(priceResult.Error);

        var courseResult = Course.Create(
            command.Name, command.Category, priceResult.Value, command.TheoryHours, command.PracticeHours, command.Description);
        if (courseResult.IsFailure)
            return Result.Failure<Guid>(courseResult.Error);

        courseRepository.Add(courseResult.Value);

        return courseResult.Value.Id;
    }
}

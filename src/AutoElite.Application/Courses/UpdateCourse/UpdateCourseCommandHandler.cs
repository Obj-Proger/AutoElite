namespace AutoElite.Application.Courses.UpdateCourse;

/// <summary>Applies the new details to the course.</summary>
internal sealed class UpdateCourseCommandHandler(ICourseRepository courseRepository, CoursePrice coursePrice)
    : ICommandHandler<UpdateCourseCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure(ApplicationErrors.NotFound<Course>());

        var priceResult = await coursePrice.CreateAsync(command.PriceAmount, command.PriceCurrency, cancellationToken);
        if (priceResult.IsFailure)
            return Result.Failure(priceResult.Error);

        return course.UpdateDetails(
            command.Name, command.Category, priceResult.Value, command.TheoryHours, command.PracticeHours, command.Description);
    }
}

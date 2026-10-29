namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithErrorCode("Test.NameEmpty").WithMessage("Name is required.");

        RuleFor(command => command.Name)
            .MaximumLength(5).WithErrorCode("Test.NameTooLong").WithMessage("Name is too long.");
    }
}

internal sealed class TestCommandNumberValidator : AbstractValidator<TestCommand>
{
    public TestCommandNumberValidator()
    {
        RuleFor(command => command.Number)
            .GreaterThan(0).WithErrorCode("Test.NumberNotPositive").WithMessage("Number must be positive.");
    }
}

internal sealed class TestCommandWithoutCodeValidator : AbstractValidator<TestCommand>
{
    public TestCommandWithoutCodeValidator()
    {
        RuleFor(command => command.Name)
            .Custom((_, context) => context.AddFailure("Something is wrong."));
    }
}

internal sealed class TestQueryValidator : AbstractValidator<TestQuery>
{
    public TestQueryValidator()
    {
        RuleFor(query => query.Value)
            .GreaterThan(0).WithErrorCode("Test.ValueNotPositive").WithMessage("Value must be positive.");
    }
}

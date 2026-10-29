namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record EmailRequest(string? Email);

internal sealed class EmailRequestValidator : AbstractValidator<EmailRequest>
{
    public EmailRequestValidator()
    {
        RuleFor(request => request.Email).ValidatedBy(Email.Create);
    }
}

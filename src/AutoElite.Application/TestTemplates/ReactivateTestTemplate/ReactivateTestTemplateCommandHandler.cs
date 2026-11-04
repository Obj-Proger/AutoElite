namespace AutoElite.Application.TestTemplates.ReactivateTestTemplate;

/// <summary>Reactivates the template.</summary>
internal sealed class ReactivateTestTemplateCommandHandler(IRepository<TestTemplate> templateRepository)
    : ICommandHandler<ReactivateTestTemplateCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateTestTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);

        return template is null ? Result.Failure(ApplicationErrors.NotFound<TestTemplate>()) : template.Reactivate();
    }
}

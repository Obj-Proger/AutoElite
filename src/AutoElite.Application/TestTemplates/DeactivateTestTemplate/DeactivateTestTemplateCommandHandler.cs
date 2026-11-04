namespace AutoElite.Application.TestTemplates.DeactivateTestTemplate;

/// <summary>Deactivates the template.</summary>
internal sealed class DeactivateTestTemplateCommandHandler(IRepository<TestTemplate> templateRepository)
    : ICommandHandler<DeactivateTestTemplateCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateTestTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);

        return template is null ? Result.Failure(ApplicationErrors.NotFound<TestTemplate>()) : template.Deactivate();
    }
}

namespace AutoElite.Application.TestTemplates.UpdateTestTemplate;

/// <summary>Builds the rules and the blocks and applies them.</summary>
internal sealed class UpdateTestTemplateCommandHandler(
    IRepository<TestTemplate> templateRepository,
    TestTemplateContent templateContent) : ICommandHandler<UpdateTestTemplateCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateTestTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result.Failure(ApplicationErrors.NotFound<TestTemplate>());

        var partsResult = await templateContent.BuildAsync(command, cancellationToken);
        if (partsResult.IsFailure)
            return partsResult;

        return template.UpdateDetails(
            command.Name, command.Category, partsResult.Value.Rules, partsResult.Value.Blocks, command.Source);
    }
}

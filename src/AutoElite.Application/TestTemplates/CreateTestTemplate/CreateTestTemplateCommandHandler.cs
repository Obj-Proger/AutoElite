namespace AutoElite.Application.TestTemplates.CreateTestTemplate;

/// <summary>Builds the rules and the blocks and creates the template.</summary>
internal sealed class CreateTestTemplateCommandHandler(
    IRepository<TestTemplate> templateRepository,
    TestTemplateContent templateContent) : ICommandHandler<CreateTestTemplateCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateTestTemplateCommand command, CancellationToken cancellationToken)
    {
        var partsResult = await templateContent.BuildAsync(command, cancellationToken);
        if (partsResult.IsFailure)
            return Result.Failure<Guid>(partsResult.Error);

        var templateResult = TestTemplate.Create(
            command.Name, command.Category, partsResult.Value.Rules, partsResult.Value.Blocks, command.Source);
        if (templateResult.IsFailure)
            return Result.Failure<Guid>(templateResult.Error);

        templateRepository.Add(templateResult.Value);

        return templateResult.Value.Id;
    }
}

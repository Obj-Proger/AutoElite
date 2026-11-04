namespace AutoElite.Application.TestTemplates.GetTestTemplates;

/// <summary>Reads the templates; a student reads only the active ones.</summary>
internal sealed class GetTestTemplatesQueryHandler(ITestTemplateQueries templateQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTestTemplatesQuery, IReadOnlyList<TestTemplateItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TestTemplateItem>>> Handle(GetTestTemplatesQuery query, CancellationToken cancellationToken)
    {
        var isActive = query.IsActive;
        if (TemplateReaders.ReadsOnlyActive(currentUser))
            isActive = true;

        return Result.Success(await templateQueries.GetListAsync(query.Category, isActive, cancellationToken));
    }
}

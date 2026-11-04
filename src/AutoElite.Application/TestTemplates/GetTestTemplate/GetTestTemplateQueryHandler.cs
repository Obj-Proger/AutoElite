namespace AutoElite.Application.TestTemplates.GetTestTemplate;

/// <summary>Reads the card and hides deactivated templates from students as not found.</summary>
internal sealed class GetTestTemplateQueryHandler(ITestTemplateQueries templateQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTestTemplateQuery, TestTemplateDetails>
{
    /// <inheritdoc />
    public async Task<Result<TestTemplateDetails>> Handle(GetTestTemplateQuery query, CancellationToken cancellationToken)
    {
        var details = await templateQueries.GetDetailsAsync(query.TemplateId, cancellationToken);

        return details is not null && (details.IsActive || !TemplateReaders.ReadsOnlyActive(currentUser))
            ? details
            : Result.Failure<TestTemplateDetails>(ApplicationErrors.NotFound<TestTemplate>());
    }
}

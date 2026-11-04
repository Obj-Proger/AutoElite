namespace AutoElite.Application.TestTemplates;

/// <summary>Reads test templates for queries.</summary>
public interface ITestTemplateQueries
{
    /// <summary>Reads the templates sorted by name. A school has few templates, so the list is not paged.</summary>
    /// <param name="category">Only templates of this licence category; <see langword="null"/> for all.</param>
    /// <param name="isActive">Only active or only deactivated templates; <see langword="null"/> for all.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<TestTemplateItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a template.</summary>
    /// <param name="templateId">The identifier of the template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the template does not exist.</returns>
    Task<TestTemplateDetails?> GetDetailsAsync(Guid templateId, CancellationToken cancellationToken = default);
}

using AutoElite.Application.TestTemplates;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeTestTemplateQueries : ITestTemplateQueries
{
    private readonly Dictionary<Guid, TestTemplateDetails> _details = [];

    public (LicenseCategory? Category, bool? IsActive)? LastListCall { get; private set; }

    public void Seed(TestTemplateDetails details) => _details.Add(details.Id, details);

    public Task<IReadOnlyList<TestTemplateItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default)
    {
        LastListCall = (category, isActive);

        return Task.FromResult<IReadOnlyList<TestTemplateItem>>([]);
    }

    public Task<TestTemplateDetails?> GetDetailsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(templateId));
}

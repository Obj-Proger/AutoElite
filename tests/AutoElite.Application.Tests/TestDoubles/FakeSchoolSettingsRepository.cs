using AutoElite.Application.Settings;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeSchoolSettingsRepository : ISchoolSettingsRepository
{
    public SchoolSettings? Settings { get; set; }

    public int AddCount { get; private set; }

    public Task<SchoolSettings?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);

    public void Add(SchoolSettings settings)
    {
        Settings = settings;
        AddCount++;
    }
}

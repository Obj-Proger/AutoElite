using AutoElite.Application.Settings;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds the single settings record of the school.</summary>
internal sealed class SchoolSettingsRepository(AutoEliteDbContext dbContext) : ISchoolSettingsRepository
{
    /// <inheritdoc />
    public Task<SchoolSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<SchoolSettings>().FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>Marks the record as the single one, so that a second record is rejected when it is saved.</remarks>
    public void Add(SchoolSettings settings)
    {
        var entry = dbContext.Set<SchoolSettings>().Add(settings);
        entry.Property(SchoolSettingsConfiguration.Singleton).CurrentValue = true;
    }
}

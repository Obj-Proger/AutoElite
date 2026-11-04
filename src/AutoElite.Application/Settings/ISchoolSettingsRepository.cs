namespace AutoElite.Application.Settings;

/// <summary>
/// Loads and adds the single settings record of the school. The infrastructure seeds the record with the defaults;
/// <see cref="SchoolSettingsProvider"/> also creates it if it is missing.
/// </summary>
public interface ISchoolSettingsRepository
{
    /// <summary>Loads the settings record.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The record, or <see langword="null"/> if the school has none yet.</returns>
    Task<SchoolSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts tracking the settings record; it is inserted on the next save.</summary>
    /// <param name="settings">The new record.</param>
    void Add(SchoolSettings settings);
}

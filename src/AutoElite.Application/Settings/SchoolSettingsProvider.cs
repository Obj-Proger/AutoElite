namespace AutoElite.Application.Settings;

/// <summary>Gives handlers the school settings that tune business rules, such as the quality window.</summary>
internal sealed class SchoolSettingsProvider(ISchoolSettingsRepository settingsRepository)
{
    /// <summary>The currency of a school that has not chosen one.</summary>
    public const string DefaultCurrencyCode = "RUB";

    /// <summary>
    /// Loads the settings, or creates them with the defaults if the school has none yet. A created record is saved
    /// together with the command that asked for it; a query only reads the defaults.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<SchoolSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is not null)
            return settings;

        settings = SchoolSettings.CreateDefault(Currency.Create(DefaultCurrencyCode).Value);
        settingsRepository.Add(settings);

        return settings;
    }
}

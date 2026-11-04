namespace AutoElite.Application.Settings.GetSchoolSettings;

/// <summary>Reads the settings, or the defaults if the school has none yet.</summary>
internal sealed class GetSchoolSettingsQueryHandler(SchoolSettingsProvider settingsProvider)
    : IQueryHandler<GetSchoolSettingsQuery, SchoolSettingsDetails>
{
    /// <inheritdoc />
    public async Task<Result<SchoolSettingsDetails>> Handle(GetSchoolSettingsQuery query, CancellationToken cancellationToken) =>
        SchoolSettingsDetails.From(await settingsProvider.GetAsync(cancellationToken));
}

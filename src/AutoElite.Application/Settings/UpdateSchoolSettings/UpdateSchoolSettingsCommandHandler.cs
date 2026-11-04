namespace AutoElite.Application.Settings.UpdateSchoolSettings;

/// <summary>Applies the values to the settings record, creating it first if the school has none yet.</summary>
internal sealed class UpdateSchoolSettingsCommandHandler(SchoolSettingsProvider settingsProvider)
    : ICommandHandler<UpdateSchoolSettingsCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateSchoolSettingsCommand command, CancellationToken cancellationToken)
    {
        var currencyResult = Currency.Create(command.DefaultCurrency);
        if (currencyResult.IsFailure)
            return Result.Failure(currencyResult.Error);

        var settings = await settingsProvider.GetAsync(cancellationToken);

        return settings.Update(
            currencyResult.Value,
            command.QualityWindowSize,
            command.LowRatingThreshold,
            TimeSpan.FromMinutes(command.StudentCancellationNoticeMinutes),
            [.. (command.LessonReminderLeadTimeMinutes ?? []).Select(minutes => TimeSpan.FromMinutes(minutes))]);
    }
}

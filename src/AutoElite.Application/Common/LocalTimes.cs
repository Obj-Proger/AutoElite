using AutoElite.Application.Settings;

namespace AutoElite.Application.Common;

/// <summary>
/// Converts UTC times to the local time of the place they belong to: the time zone of the branch, or the school's
/// time zone for records not tied to a branch. Texts that people read, such as notifications, show local times.
/// </summary>
internal sealed class LocalTimes(IRepository<Branch> branchRepository, ISchoolSettingsRepository settingsRepository)
{
    /// <summary>Converts a UTC time to the local time of the branch.</summary>
    /// <param name="utcTime">The UTC time.</param>
    /// <param name="branchId">The branch of the record the time belongs to, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</returns>
    public async Task<DateTime> ToLocalAsync(DateTime utcTime, Guid? branchId, CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(await GetTimeZoneIdAsync(branchId, cancellationToken));

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), timeZone);
    }

    private async Task<string> GetTimeZoneIdAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { } id && await branchRepository.GetByIdAsync(id, cancellationToken) is { } branch)
            return branch.TimeZoneId;

        var settings = await settingsRepository.GetAsync(cancellationToken);

        return settings?.TimeZoneId ?? SchoolSettings.DefaultTimeZoneId;
    }
}

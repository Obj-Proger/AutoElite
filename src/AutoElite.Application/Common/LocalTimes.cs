using AutoElite.Application.Settings;

namespace AutoElite.Application.Common;

/// <summary>
/// Converts UTC times to the local time of the place they belong to: the time zone of the branch, or the school's
/// time zone for records not tied to a branch. Texts that people read, such as notifications, show local times.
/// </summary>
/// <remarks>The time zones are looked up once per branch for the lifetime of the service, one request.</remarks>
internal sealed class LocalTimes(IRepository<Branch> branchRepository, ISchoolSettingsRepository settingsRepository)
{
    private readonly Dictionary<Guid, TimeZoneInfo> _branchTimeZones = [];
    private TimeZoneInfo? _schoolTimeZone;

    /// <summary>Converts a UTC time to the local time of the branch.</summary>
    /// <param name="utcTime">The UTC time.</param>
    /// <param name="branchId">The branch of the record the time belongs to, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</returns>
    public async Task<DateTime> ToLocalAsync(DateTime utcTime, Guid? branchId, CancellationToken cancellationToken)
    {
        var timeZone = await GetTimeZoneAsync(branchId, cancellationToken);

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), timeZone);
    }

    /// <summary>Gets the time zone of the branch, or the school's time zone for records not tied to a branch.</summary>
    /// <param name="branchId">The branch, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<TimeZoneInfo> GetTimeZoneAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        if (branchId is not { } id)
            return await GetSchoolTimeZoneAsync(cancellationToken);

        if (_branchTimeZones.TryGetValue(id, out var cached))
            return cached;

        var branch = await branchRepository.GetByIdAsync(id, cancellationToken);
        var timeZone = branch is null
            ? await GetSchoolTimeZoneAsync(cancellationToken)
            : TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
        _branchTimeZones[id] = timeZone;

        return timeZone;
    }

    private async Task<TimeZoneInfo> GetSchoolTimeZoneAsync(CancellationToken cancellationToken)
    {
        if (_schoolTimeZone is null)
        {
            var settings = await settingsRepository.GetAsync(cancellationToken);
            _schoolTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings?.TimeZoneId ?? SchoolSettings.DefaultTimeZoneId);
        }

        return _schoolTimeZone;
    }
}

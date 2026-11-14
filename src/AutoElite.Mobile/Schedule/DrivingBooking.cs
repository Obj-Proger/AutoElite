using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// What a student needs to book a driving lesson: their active contracts and the free slots a contract can book in the
/// next 30 days, read from the API or, without a network, from the device. The API decides each booking when it
/// receives it, since a slot may be taken meanwhile.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class DrivingBooking(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(30);

    /// <summary>Reads the active contracts of the student.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The contracts.</returns>
    public async Task<Cached<IReadOnlyList<ContractListItem>>> ReadActiveContractsAsync(CancellationToken cancellationToken)
    {
        var contracts = await cache.GetListAsync(
            "me/contracts",
            token => createApi().Api.V1.Me.Contracts.GetAsync(cancellationToken: token),
            ContractListItem.CreateFromDiscriminatorValue,
            cancellationToken);

        return contracts with { Value = [.. contracts.Value.Where(contract => contract.Status == ContractStatus.Active)] };
    }

    /// <summary>Reads the free slots a contract can book in the next 30 days, the earliest first.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots.</returns>
    public Task<Cached<IReadOnlyList<DrivingSlotItem>>> ReadAvailableSlotsAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow();

        return ReadAvailableSlotsAsync(
            string.Create(CultureInfo.InvariantCulture, $"driving-slots/available/{contractId:N}"),
            contractId,
            fromUtc,
            fromUtc + _ahead,
            cancellationToken);
    }

    /// <summary>Reads the free slots a contract can book in what is left of a week, for the schedule.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="firstDay">The first day of the week.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots, none when the week is over.</returns>
    public async Task<IReadOnlyList<DrivingSlotItem>> ReadAvailableWeekAsync(
        Guid contractId, DateOnly firstDay, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var start = ScheduleWeek.StartOf(firstDay);
        var end = ScheduleWeek.StartOf(firstDay.AddDays(ScheduleWeek.Days));
        if (end <= now)
            return [];

        var key = ScheduleWeek.KeyOf(string.Create(CultureInfo.InvariantCulture, $"driving-slots/available/{contractId:N}"), firstDay);
        var slots = await ReadAvailableSlotsAsync(key, contractId, start > now ? start : now, end, cancellationToken);

        return slots.Value;
    }

    private Task<Cached<IReadOnlyList<DrivingSlotItem>>> ReadAvailableSlotsAsync(
        string key, Guid contractId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            key,
            token => createApi().Api.V1.DrivingSlots.Available.GetAsync(
                request =>
                {
                    request.QueryParameters.ContractId = contractId;
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            DrivingSlotItem.CreateFromDiscriminatorValue,
            cancellationToken);
}

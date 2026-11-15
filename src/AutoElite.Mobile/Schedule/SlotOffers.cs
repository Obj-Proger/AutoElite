using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The practice slots a driving instructor offers: a new slot with a vehicle and, if wanted, a training ground, a slot
/// withdrawn with a reason, and the slots of a week repeated on the next weeks. These need the network, since the API
/// checks that the instructor and the vehicle are free at that time; the vehicles and grounds to choose from are read
/// from the API or, without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="schedule">The slots of the instructor, to find the times already taken.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class SlotOffers(
    OfflineCache cache, Func<AutoEliteApiClient> createApi, InstructorSchedule schedule, TimeProvider timeProvider)
{
    /// <summary>Reads the vehicles in service the instructor can offer slots with.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The vehicles.</returns>
    public Task<Cached<IReadOnlyList<VehicleItem>>> ReadVehiclesAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "vehicles/active",
            token => createApi().Api.V1.Vehicles.GetAsync(request => request.QueryParameters.IsActive = true, token),
            VehicleItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the training grounds in use where a lesson can start.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The grounds.</returns>
    public Task<Cached<IReadOnlyList<TrainingGroundItem>>> ReadGroundsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "training-grounds/active",
            token => createApi().Api.V1.TrainingGrounds.GetAsync(request => request.QueryParameters.IsActive = true, token),
            TrainingGroundItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Offers a slot that students whose contract matches the vehicle can book.</summary>
    /// <param name="vehicleId">The vehicle.</param>
    /// <param name="start">The start, in the future.</param>
    /// <param name="end">The end; a slot lasts at most 4 hours.</param>
    /// <param name="groundId">The ground where the lesson starts, or <see langword="null"/>.</param>
    /// <param name="routeId">The route of the lesson, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the slot is offered.</returns>
    public Task OfferAsync(
        Guid vehicleId,
        DateTimeOffset start,
        DateTimeOffset end,
        Guid? groundId,
        Guid? routeId,
        CancellationToken cancellationToken) =>
        createApi().Api.V1.DrivingSlots.PostAsync(
            new OfferDrivingSlotCommand
            {
                VehicleId = vehicleId,
                StartUtc = start.ToUniversalTime(),
                EndUtc = end.ToUniversalTime(),
                TrainingGroundId = groundId,
                RouteId = routeId
            },
            cancellationToken: cancellationToken);

    /// <summary>Withdraws a slot; its booking, if any, is cancelled and the student is told why.</summary>
    /// <param name="slotId">The slot.</param>
    /// <param name="reason">Why the slot is withdrawn.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the slot is withdrawn.</returns>
    public Task WithdrawAsync(Guid slotId, string reason, CancellationToken cancellationToken) =>
        createApi().Api.V1.DrivingSlots[slotId].Withdraw.PostAsync(
            new WithdrawDrivingSlotCommand { SlotId = slotId, Reason = reason }, cancellationToken: cancellationToken);

    /// <summary>
    /// Repeats the slots of a week on the next weeks, at the same days and hours, with the same vehicles, grounds and
    /// routes: booked slots are repeated free. A copy is left out when it would start in the past or overlap a slot the
    /// instructor already has then; the API refuses one whose vehicle is taken.
    /// </summary>
    /// <param name="firstDay">The first day of the week to repeat.</param>
    /// <param name="weeks">The number of weeks after it, from 1 to 8.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>How many slots were offered and how many were left out.</returns>
    public async Task<(int Offered, int Skipped)> RepeatWeekAsync(DateOnly firstDay, int weeks, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(weeks, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(weeks, 8);
        var source = await ReadSlotsAsync(firstDay, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var (offered, skipped) = (0, 0);
        for (var week = 1; week <= weeks; week++)
        {
            var shift = TimeSpan.FromDays(ScheduleWeek.Days * week);
            var taken = await ReadSlotsAsync(firstDay.AddDays(ScheduleWeek.Days * week), cancellationToken);
            foreach (var slot in source)
            {
                var (start, end) = (slot.StartUtc!.Value + shift, slot.EndUtc!.Value + shift);
                if (start <= now || taken.Any(other => other.StartUtc < end && start < other.EndUtc))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    await OfferAsync(slot.VehicleId!.Value, start, end, slot.TrainingGroundId, slot.RouteId, cancellationToken);
                    offered++;
                }
                catch (ApiException exception) when (exception.ResponseStatusCode is >= 400 and < 500)
                {
                    // The vehicle is taken or no longer in service: this copy is left out, the others go on.
                    skipped++;
                }
            }
        }

        return (offered, skipped);
    }

    // The slots of a week that are not withdrawn, read from the API, since repeating needs what is there now.
    private async Task<List<DrivingSlotItem>> ReadSlotsAsync(DateOnly firstDay, CancellationToken cancellationToken)
    {
        var week = await schedule.ReadDrivingWeekAsync(firstDay, cancellationToken);
        if (week.IsFromCache)
            throw new HttpRequestException("The API cannot be reached.");

        return
        [
            .. week.Value
                .Select(item => item.Slot)
                .OfType<DrivingSlotItem>()
                .Where(slot => slot is { StartUtc: not null, EndUtc: not null, VehicleId: not null }
                    && slot.Status != DrivingSlotStatus.Withdrawn)
        ];
    }
}

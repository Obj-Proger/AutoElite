using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A new practice slot of a driving instructor: its day, start and length, the vehicle, which decides the category
/// and the gearbox of the students who can book it, and the training ground where the lesson starts, if any. The API
/// checks that the instructor and the vehicle are free then; offering needs the network.
/// </summary>
public sealed class OfferSlotViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="SlotRequest"/>.</summary>
    public const string SlotParameter = "slot";

    // The days ahead a slot can be offered on, the hours of the day it can start at, and the lengths it can have.
    private const int DaysAhead = 60;
    private const int FirstHour = 6;
    private const int LastHour = 22;
    private static readonly int[] _lengths = [60, 90, 120, 180, 240];

    private readonly SlotOffers _offers;
    private readonly TimeProvider _timeProvider;
    private IReadOnlyList<DateOnly> _days = [];
    private IReadOnlyList<TimeOnly> _times = [];
    private IReadOnlyList<Guid> _vehicleIds = [];
    private IReadOnlyList<Guid?> _groundIds = [];

    /// <summary>Initializes a new instance of the <see cref="OfferSlotViewModel"/> class.</summary>
    /// <param name="offers">The slots of the instructor.</param>
    /// <param name="timeProvider">The clock.</param>
    public OfferSlotViewModel(SlotOffers offers, TimeProvider timeProvider)
    {
        _offers = offers;
        _timeProvider = timeProvider;
        Lengths = [.. _lengths.Select(LengthName)];
        LengthIndex = Array.IndexOf(_lengths, 120);
        OfferCommand = new Command(async () => await OfferAsync(), () => !IsBusy);
        Choose(_timeProvider.GetLocalNow().AddHours(1));
    }

    /// <summary>Gets the days the slot can be on, from today.</summary>
    public IReadOnlyList<string> Days
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the day, by its position.</summary>
    public int DayIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the times the slot can start at, every half hour.</summary>
    public IReadOnlyList<string> Times
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the start, by its position.</summary>
    public int TimeIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the lengths the slot can have, up to 4 hours.</summary>
    public IReadOnlyList<string> Lengths { get; }

    /// <summary>Gets or sets the length, by its position.</summary>
    public int LengthIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the vehicles in service, by name and plate.</summary>
    public IReadOnlyList<string> Vehicles
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the vehicle, by its position.</summary>
    public int VehicleIndex
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    /// <summary>Gets the training grounds, after the choice of none.</summary>
    public IReadOnlyList<string> Grounds
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the training ground, by its position; the first is none.</summary>
    public int GroundIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets why the slot could not be offered, or that the lists are from the device, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether the slot is being offered.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OfferCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that offers the slot.</summary>
    public Command OfferCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(SlotParameter, out var value) && value is SlotRequest request)
            Choose(request.Start);
    }

    /// <summary>Reads the vehicles and the training grounds.</summary>
    /// <returns>A task that completes when they can be chosen.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var vehicles = await _offers.ReadVehiclesAsync(CancellationToken.None);
            var grounds = await _offers.ReadGroundsAsync(CancellationToken.None);
            List<VehicleItem> usable = [.. vehicles.Value.Where(vehicle => vehicle.Id is not null)];
            _vehicleIds = [.. usable.Select(vehicle => vehicle.Id!.Value)];
            Vehicles = [.. usable.Select(VehicleName)];
            VehicleIndex = usable.Count == 1 ? 0 : -1;
            List<TrainingGroundItem> places = [.. grounds.Value.Where(ground => ground.Id is not null)];
            _groundIds = [null, .. places.Select(ground => ground.Id)];
            Grounds = [AppTexts.Get("OfferSlot.NoGround"), .. places.Select(ground => ground.Name ?? string.Empty)];
            GroundIndex = 0;
            Message = usable.Count == 0
                ? AppTexts.Get("OfferSlot.NoVehicles")
                : vehicles.IsFromCache ? AppTexts.Get("OfferSlot.Offline") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Message = ProblemTexts.Of(exception);
        }
    }

    private static string LengthName(int minutes) =>
        AppTexts.Format("OfferSlot.Hours", (minutes / 60d).ToString("0.#", CultureInfo.CurrentCulture));

    private static string VehicleName(VehicleItem vehicle) =>
        string.Join(
            " · ",
            new[]
            {
                string.Join(' ', new[] { vehicle.Make, vehicle.Model }.Where(part => !string.IsNullOrWhiteSpace(part))),
                vehicle.Plate,
                vehicle.Transmission == Transmission.Automatic ? AppTexts.Get("OfferSlot.Automatic") : AppTexts.Get("OfferSlot.Manual")
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

    // Fills the days and times so that the start chosen on the schedule is among them, at the nearest half hour allowed.
    private void Choose(DateTimeOffset start)
    {
        var culture = CultureInfo.CurrentCulture;
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var local = start.ToLocalTime();
        var day = DateOnly.FromDateTime(local.DateTime);
        if (day < today)
            day = today;

        _days = [.. Enumerable.Range(0, Math.Max(DaysAhead, day.DayNumber - today.DayNumber + 1)).Select(today.AddDays)];
        Days = [.. _days.Select(date => date.ToString("ddd, d MMMM", culture))];
        DayIndex = day.DayNumber - today.DayNumber;

        _times =
        [
            .. Enumerable.Range(0, ((LastHour - FirstHour) * 2) + 1).Select(half => new TimeOnly(FirstHour, 0).AddMinutes(half * 30))
        ];
        Times = [.. _times.Select(time => time.ToString("t", culture))];
        var halfHours = ((local.Hour * 60) + local.Minute) / 30;
        TimeIndex = Math.Clamp(halfHours - (FirstHour * 2), 0, _times.Count - 1);
    }

    private async Task OfferAsync()
    {
        if (VehicleIndex < 0 || VehicleIndex >= _vehicleIds.Count)
        {
            Message = AppTexts.Get("OfferSlot.ChooseVehicle");

            return;
        }

        var startLocal = _days[Math.Max(0, DayIndex)].ToDateTime(_times[Math.Max(0, TimeIndex)]);
        var start = new DateTimeOffset(startLocal);
        var end = start.AddMinutes(_lengths[Math.Max(0, LengthIndex)]);
        if (start <= _timeProvider.GetUtcNow())
        {
            Message = AppTexts.Get("OfferSlot.InThePast");

            return;
        }

        IsBusy = true;
        Message = null;
        try
        {
            var groundId = GroundIndex > 0 && GroundIndex < _groundIds.Count ? _groundIds[GroundIndex] : null;
            await _offers.OfferAsync(_vehicleIds[VehicleIndex], start, end, groundId, null, CancellationToken.None);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// Booking a driving lesson: the student chooses one of their active contracts, when they have more than one, and a
/// free slot of the next 30 days. The booking goes through the offline queue: it reaches the school at once when the
/// device is online and later otherwise, and the school may still refuse it when the slot was taken meanwhile.
/// </summary>
public sealed class BookingViewModel : ViewModel
{
    private readonly DrivingBooking _booking;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly OfflineQueue _queue;
    private Guid? _contractId;

    /// <summary>Initializes a new instance of the <see cref="BookingViewModel"/> class.</summary>
    /// <param name="booking">The contracts and the free slots.</param>
    /// <param name="synchronizer">Sends the booking.</param>
    /// <param name="queue">The actions that wait to be sent, where a refused booking is removed once it is shown.</param>
    public BookingViewModel(DrivingBooking booking, OfflineSynchronizer synchronizer, OfflineQueue queue)
    {
        _booking = booking;
        _synchronizer = synchronizer;
        _queue = queue;
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the active contracts, offered for choosing when there is more than one.</summary>
    public IReadOnlyList<ContractOption> Contracts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasChoice));
        }
    } = [];

    /// <summary>Gets a value indicating whether the student chooses between contracts.</summary>
    public bool HasChoice => Contracts.Count > 1;

    /// <summary>Gets the contract the slots are for.</summary>
    public string? Contract
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the free slots of the contract, the earliest first.</summary>
    public IReadOnlyList<SlotEntry> Slots
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets why there are no slots to show, or that the slots are from the device, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice to show.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets or sets a value indicating whether the page is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that reads the contracts and the slots again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the contracts and the slots of the chosen one, the first contract at first.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        IReadOnlyList<ContractListItem> contracts;
        try
        {
            contracts = (await _booking.ReadActiveContractsAsync(CancellationToken.None)).Value;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Show([], null, AppTexts.Get("Booking.Unavailable"));

            return;
        }

        Contracts =
        [
            .. contracts.Where(contract => contract.Id is not null).Select(contract => new ContractOption
            {
                Course = contract.CourseName ?? string.Empty,
                Number = contract.Number ?? string.Empty,
                SelectCommand = new Command(async () => await SelectAsync(contract))
            })
        ];

        var chosen = contracts.FirstOrDefault(contract => contract.Id == _contractId)
            ?? contracts.FirstOrDefault(contract => contract.Id is not null);
        if (chosen is null)
        {
            Show([], null, AppTexts.Get("Booking.NoContract"));

            return;
        }

        await SelectAsync(chosen);
    }

    private async Task SelectAsync(ContractListItem contract)
    {
        if (contract.Id is not { } contractId)
            return;

        _contractId = contractId;
        var name = LessonTexts.DetailsOf(contract.CourseName, contract.Number);
        Cached<IReadOnlyList<DrivingSlotItem>> slots;
        try
        {
            slots = await _booking.ReadAvailableSlotsAsync(contractId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Show([], name, AppTexts.Get("Booking.Unavailable"));

            return;
        }

        var entries = slots.Value.Select(slot => Entry(contractId, slot)).OfType<SlotEntry>().ToList();
        var notice = slots.IsFromCache
            ? AppTexts.Format("Booking.Offline", slots.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            : entries.Count == 0 ? AppTexts.Get("Booking.Empty") : null;
        Show(entries, name, notice);
    }

    private void Show(IReadOnlyList<SlotEntry> slots, string? contract, string? notice)
    {
        Slots = slots;
        Contract = contract;
        Notice = notice;
    }

    private SlotEntry? Entry(Guid contractId, DrivingSlotItem slot)
    {
        if (slot is not { Id: { } slotId, StartUtc: { } start, EndUtc: { } end })
            return null;

        var time = LessonTexts.TimeOf(start, end);
        var details = LessonTexts.DetailsOf(slot.InstructorName, slot.TrainingGroundName);

        return new SlotEntry
        {
            Time = time,
            Vehicle = LessonTexts.DetailsOf(slot.VehicleName, slot.VehiclePlate),
            Details = details,
            BookCommand = new Command(async () => await BookAsync(new BookDrivingSlot(slotId, contractId), time, details))
        };
    }

    private async Task BookAsync(BookDrivingSlot booking, string time, string details)
    {
        if (!await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Booking.ConfirmTitle"),
            LessonTexts.DetailsOf(time, details),
            AppTexts.Get("Booking.Book"),
            AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        var result = await _synchronizer.SubmitAsync(booking, CancellationToken.None);
        var refused = (await _queue.ListAsync(CancellationToken.None))
            .FirstOrDefault(action => action.IsRejected && action.Action == booking);
        if (refused is not null)
        {
            // Shown here, so it is not listed again on the home page.
            await _queue.DismissAsync(refused.Id, CancellationToken.None);
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Booking.Title"), AppTexts.Get("Booking.Refused"), AppTexts.Get("Common.Ok"));
            await LoadAsync();

            return;
        }

        await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Booking.Title"),
            result.IsComplete ? AppTexts.Get("Booking.Booked") : AppTexts.Get("Booking.Queued"),
            AppTexts.Get("Common.Ok"));
        await Shell.Current.GoToAsync("..");
    }

    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}

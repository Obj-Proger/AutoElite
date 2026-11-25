using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>A booked driving lesson that a manager moves to another slot, passed to the page that moves it.</summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="ContractId">The contract the lesson is booked on, which books the new slot.</param>
/// <param name="StudentName">The name of the student.</param>
/// <param name="Summary">When, with whom and on what the lesson is, shown at the top of the page.</param>
/// <param name="Category">The licence category of the vehicle, which the new slot keeps.</param>
/// <param name="Transmission">The gearbox of the vehicle, which the new slot keeps.</param>
public sealed record MoveLessonRequest(
    Guid SlotId, Guid ContractId, string StudentName, string Summary, LicenseCategory? Category, Transmission? Transmission);

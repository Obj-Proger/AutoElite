using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>A free practice slot for which a manager books a student, passed to the page that books.</summary>
/// <param name="SlotId">The slot.</param>
/// <param name="Summary">When, with whom and on what the lesson is, shown at the top of the page.</param>
/// <param name="Category">The licence category of the vehicle, which the contract must be for.</param>
/// <param name="Transmission">The gearbox of the vehicle, which the contract must train on.</param>
public sealed record BookStudentRequest(Guid SlotId, string Summary, LicenseCategory? Category, Transmission? Transmission);

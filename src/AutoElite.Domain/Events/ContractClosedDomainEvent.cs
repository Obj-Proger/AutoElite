using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>
/// Raised when a contract is completed or terminated; the student leaves the study group and future
/// bookings are released in response.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="Status">The final status: <see cref="ContractStatus.Completed"/> or <see cref="ContractStatus.Terminated"/>.</param>
/// <param name="OccurredOnUtc">The UTC time at which the contract was closed.</param>
public sealed record ContractClosedDomainEvent(Guid ContractId, Guid StudentId, ContractStatus Status, DateTime OccurredOnUtc) : IDomainEvent;

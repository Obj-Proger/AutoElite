namespace AutoElite.Application.Contracts.ChangeContractTransmission;

/// <summary>
/// Switches the gearbox of an active contract, for example when the student moves to an automatic. Lessons booked
/// afterwards must be on vehicles with the new gearbox.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="Transmission">The new gearbox.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ChangeContractTransmissionCommand(Guid ContractId, Transmission Transmission) : ICommand;

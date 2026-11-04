using AutoElite.Application.Vehicles;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeVehicleRepository : InMemoryRepository<Vehicle>, IVehicleRepository
{
    public Task<bool> PlateExistsAsync(LicensePlate plate, Guid? exceptVehicleId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(vehicle => vehicle.Id != exceptVehicleId && vehicle.Plate == plate));
}

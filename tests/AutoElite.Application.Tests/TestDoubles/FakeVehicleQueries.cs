using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Vehicles;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded vehicles and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeVehicleQueries : IVehicleQueries
{
    private readonly Dictionary<Guid, VehicleItem> _vehicles = [];

    public (VehicleListFilter Filter, DataScope Scope)? LastListCall { get; private set; }

    public void Seed(VehicleItem vehicle) => _vehicles.Add(vehicle.Id, vehicle);

    public Task<IReadOnlyList<VehicleItem>> GetListAsync(
        VehicleListFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<VehicleItem>>([.. _vehicles.Values]);
    }

    public Task<VehicleItem?> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_vehicles.GetValueOrDefault(vehicleId));
}

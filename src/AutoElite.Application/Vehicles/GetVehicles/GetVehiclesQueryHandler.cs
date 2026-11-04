namespace AutoElite.Application.Vehicles.GetVehicles;

/// <summary>Reads the fleet within the caller's scope; a branch outside the scope gives an empty list.</summary>
internal sealed class GetVehiclesQueryHandler(IVehicleQueries vehicleQueries, ICurrentUser currentUser)
    : IQueryHandler<GetVehiclesQuery, IReadOnlyList<VehicleItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VehicleItem>>> Handle(GetVehiclesQuery query, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);
        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return Result.Success<IReadOnlyList<VehicleItem>>([]);

        var filter = new VehicleListFilter(query.Category, query.Transmission, query.BranchId, query.IsActive);

        return Result.Success(await vehicleQueries.GetListAsync(filter, scope, cancellationToken));
    }
}

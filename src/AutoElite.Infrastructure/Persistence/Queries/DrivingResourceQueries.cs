using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.TrainingGrounds;
using AutoElite.Application.Vehicles;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads the fleet without tracking; a school's fleet is small, so the list is read whole.</summary>
internal sealed class VehicleQueries(AutoEliteDbContext dbContext) : IVehicleQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleItem>> GetListAsync(
        VehicleListFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var vehicles = dbContext.Set<Vehicle>().AsNoTracking();
        if (!scope.IsSchoolWide)
            vehicles = vehicles.Where(vehicle => vehicle.BranchId == null || vehicle.BranchId == scope.BranchId);

        if (filter.Category is { } category)
            vehicles = vehicles.Where(vehicle => vehicle.Category == category);

        if (filter.Transmission is { } transmission)
            vehicles = vehicles.Where(vehicle => vehicle.Transmission == transmission);

        if (filter.BranchId is { } branchId)
            vehicles = vehicles.Where(vehicle => vehicle.BranchId == branchId);

        if (filter.IsActive is { } isActive)
            vehicles = vehicles.Where(vehicle => vehicle.IsActive == isActive);

        return await ReadAsync(vehicles, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VehicleItem?> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        (await ReadAsync(dbContext.Set<Vehicle>().AsNoTracking().Where(vehicle => vehicle.Id == vehicleId), cancellationToken))
            .SingleOrDefault();

    // The plate is a converted value object: it is read as such and its text taken in memory.
    private async Task<List<VehicleItem>> ReadAsync(IQueryable<Vehicle> vehicles, CancellationToken cancellationToken)
    {
        var rows = await (
                from vehicle in vehicles
                join branch in dbContext.Set<Branch>() on vehicle.BranchId equals (Guid?)branch.Id into branches
                from branch in branches.DefaultIfEmpty()
                orderby vehicle.Plate
                select new { Vehicle = vehicle, BranchName = branch == null ? null : branch.Name })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new VehicleItem(
                row.Vehicle.Id, row.Vehicle.Plate.Value, row.Vehicle.Make, row.Vehicle.Model, row.Vehicle.Year,
                row.Vehicle.Category, row.Vehicle.Transmission, row.Vehicle.BranchId, row.BranchName, row.Vehicle.IsActive))
        ];
    }
}

/// <summary>Reads training grounds without tracking.</summary>
internal sealed class TrainingGroundQueries(AutoEliteDbContext dbContext) : ITrainingGroundQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TrainingGroundItem>> GetListAsync(
        Guid? branchId, bool? isActive, DataScope scope, CancellationToken cancellationToken = default)
    {
        var grounds = dbContext.Set<TrainingGround>().AsNoTracking();
        if (!scope.IsSchoolWide)
            grounds = grounds.Where(ground => ground.BranchId == null || ground.BranchId == scope.BranchId);

        if (branchId is { } branch)
            grounds = grounds.Where(ground => ground.BranchId == branch);

        if (isActive is { } active)
            grounds = grounds.Where(ground => ground.IsActive == active);

        return await Items(grounds).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TrainingGroundItem?> GetAsync(Guid groundId, CancellationToken cancellationToken = default) =>
        Items(dbContext.Set<TrainingGround>().AsNoTracking().Where(ground => ground.Id == groundId))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<TrainingGroundItem> Items(IQueryable<TrainingGround> grounds) =>
        from ground in grounds
        join branch in dbContext.Set<Branch>() on ground.BranchId equals (Guid?)branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        orderby ground.Name, ground.Id
        select new TrainingGroundItem(
            ground.Id,
            ground.Name,
            ground.Address.City,
            ground.Address.Street,
            ground.Description,
            ground.BranchId,
            branch == null ? null : branch.Name,
            ground.IsActive);
}

/// <summary>
/// Reads driving routes without tracking; a route belongs to the branch of its ground, and a route without a ground or
/// on a shared ground is in every scope.
/// </summary>
internal sealed class DrivingRouteQueries(AutoEliteDbContext dbContext) : IDrivingRouteQueries
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DrivingRouteItem>> GetListAsync(
        DrivingRouteListFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        var rows = Rows();
        if (!scope.IsSchoolWide)
            rows = rows.Where(row => row.BranchId == null || row.BranchId == scope.BranchId);

        if (filter.AuthorId is { } authorId)
            rows = rows.Where(row => row.AuthorId == authorId);

        if (filter.TrainingGroundId is { } groundId)
            rows = rows.Where(row => row.TrainingGroundId == groundId);

        if (filter.IsActive is { } isActive)
            rows = rows.Where(row => row.IsActive == isActive);

        var found = await rows.OrderBy(row => row.Name).ThenBy(row => row.Id).ToListAsync(cancellationToken);

        return [.. found.Select(ToItem)];
    }

    /// <inheritdoc />
    public async Task<DrivingRouteItem?> GetAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var row = await Rows().FirstOrDefaultAsync(found => found.Id == routeId, cancellationToken);

        return row is null ? null : ToItem(row);
    }

    private static DrivingRouteItem ToItem(RouteRow row) =>
        new(
            row.Id, row.Name, row.Description, row.AuthorId,
            PersonNames.Format(row.AuthorLastName, row.AuthorFirstName, row.AuthorMiddleName)!, row.TrainingGroundId,
            row.TrainingGroundName, row.BranchId, row.IsActive);

    private IQueryable<RouteRow> Rows() =>
        from route in dbContext.Set<DrivingRoute>().AsNoTracking()
        join author in dbContext.Set<User>() on route.AuthorId equals author.Id
        join ground in dbContext.Set<TrainingGround>() on route.TrainingGroundId equals (Guid?)ground.Id into grounds
        from ground in grounds.DefaultIfEmpty()
        select new RouteRow
        {
            Id = route.Id,
            Name = route.Name,
            Description = route.Description,
            AuthorId = route.AuthorId,
            AuthorLastName = author.FullName.LastName,
            AuthorFirstName = author.FullName.FirstName,
            AuthorMiddleName = author.FullName.MiddleName,
            TrainingGroundId = route.TrainingGroundId,
            TrainingGroundName = ground == null ? null : ground.Name,
            BranchId = ground == null ? null : ground.BranchId,
            IsActive = route.IsActive
        };

    /// <summary>The columns of a route that the read model is built from.</summary>
    private sealed class RouteRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = null!;

        public string Description { get; init; } = null!;

        public Guid AuthorId { get; init; }

        public string AuthorLastName { get; init; } = null!;

        public string AuthorFirstName { get; init; } = null!;

        public string? AuthorMiddleName { get; init; }

        public Guid? TrainingGroundId { get; init; }

        public string? TrainingGroundName { get; init; }

        public Guid? BranchId { get; init; }

        public bool IsActive { get; init; }
    }
}

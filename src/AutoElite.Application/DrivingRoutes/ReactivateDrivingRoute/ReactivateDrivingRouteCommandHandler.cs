namespace AutoElite.Application.DrivingRoutes.ReactivateDrivingRoute;

/// <summary>Reactivates the route.</summary>
internal sealed class ReactivateDrivingRouteCommandHandler(DrivingRouteAccess routeAccess) : ICommandHandler<ReactivateDrivingRouteCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateDrivingRouteCommand command, CancellationToken cancellationToken)
    {
        var routeResult = await routeAccess.LoadAsync(command.RouteId, cancellationToken);

        return routeResult.IsFailure ? routeResult : routeResult.Value.Reactivate();
    }
}

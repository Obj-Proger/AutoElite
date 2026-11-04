namespace AutoElite.Application.DrivingRoutes.DeactivateDrivingRoute;

/// <summary>Deactivates the route.</summary>
internal sealed class DeactivateDrivingRouteCommandHandler(DrivingRouteAccess routeAccess) : ICommandHandler<DeactivateDrivingRouteCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateDrivingRouteCommand command, CancellationToken cancellationToken)
    {
        var routeResult = await routeAccess.LoadAsync(command.RouteId, cancellationToken);

        return routeResult.IsFailure ? routeResult : routeResult.Value.Deactivate();
    }
}

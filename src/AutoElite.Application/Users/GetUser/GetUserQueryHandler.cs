namespace AutoElite.Application.Users.GetUser;

/// <summary>Reads the card and hides users outside the caller's scope as not found.</summary>
internal sealed class GetUserQueryHandler(IUserQueries userQueries, ICurrentUser currentUser)
    : IQueryHandler<GetUserQuery, UserDetails>
{
    /// <inheritdoc />
    public async Task<Result<UserDetails>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var details = await userQueries.GetDetailsAsync(query.UserId, cancellationToken);
        if (details is null || !DataScope.For(currentUser).Includes(details.BranchId))
            return Result.Failure<UserDetails>(ApplicationErrors.NotFound<User>());

        return details;
    }
}

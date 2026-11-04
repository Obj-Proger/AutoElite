namespace AutoElite.Application.Users.GetMyProfile;

/// <summary>Reads the profile of the user the request runs for.</summary>
internal sealed class GetMyProfileQueryHandler(IUserQueries userQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyProfileQuery, UserProfile>
{
    /// <inheritdoc />
    public async Task<Result<UserProfile>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await userQueries.GetProfileAsync(currentUser.GetRequiredUserId(), cancellationToken);

        return profile is null ? Result.Failure<UserProfile>(ApplicationErrors.NotFound<User>()) : profile;
    }
}

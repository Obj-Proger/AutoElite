namespace AutoElite.Application.Users.GetMyProfile;

/// <summary>Reads the profile of the signed-in user.</summary>
public sealed record GetMyProfileQuery : IQuery<UserProfile>;

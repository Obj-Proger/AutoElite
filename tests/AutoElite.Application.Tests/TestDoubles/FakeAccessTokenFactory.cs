using AutoElite.Application.Users.Authentication;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeAccessTokenFactory : IAccessTokenFactory
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public IssuedToken Create(User user, IReadOnlyCollection<UserRole> roles, DateTime utcNow) =>
        new($"access:{user.Id}:{string.Join(',', roles)}", utcNow + Lifetime);
}

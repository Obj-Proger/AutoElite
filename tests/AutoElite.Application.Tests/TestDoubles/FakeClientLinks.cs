using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeClientLinks : IClientLinks
{
    public Uri Invitation(Guid userId, string token) => new($"https://app.test/invitation?user={userId}&token={token}");

    public Uri PasswordReset(Guid userId, string token) => new($"https://app.test/reset-password?user={userId}&token={token}");
}

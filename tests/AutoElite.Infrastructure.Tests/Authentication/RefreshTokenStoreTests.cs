using AutoElite.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.Authentication;

public sealed class RefreshTokenStoreTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly IOptions<JwtOptions> _options = Options.Create(new JwtOptions { RefreshTokenLifetime = TimeSpan.FromDays(30) });

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateTime Now => FixedClock.Default.UtcNow;

    private async Task<Guid> SaveUserAsync()
    {
        var user = User.Create(
                FullName.Create("Иванов", "Иван").Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value, null, null, Now)
            .Value;
        await using var context = database.CreateContext();
        context.Add(user);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);

        return user.Id;
    }

    /// <summary>Runs one request with the store and saves it.</summary>
    private async Task<T> RequestAsync<T>(Func<RefreshTokenStore, Task<T>> action)
    {
        await using var context = database.CreateContext();
        var result = await action(new RefreshTokenStore(context, _options));
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);

        return result;
    }

    private Task<string> IssueAsync(Guid userId) =>
        RequestAsync(async store => (await store.IssueAsync(userId, Now, CancellationToken)).Value);

    private Task<Guid?> RedeemAsync(string token, DateTime? utcNow = null) =>
        RequestAsync(store => store.RedeemAsync(token, utcNow ?? Now, CancellationToken));

    [Fact]
    public async Task Issue_StoresOnlyTheHashOfTheToken()
    {
        var userId = await SaveUserAsync();

        var issued = await RequestAsync(store => store.IssueAsync(userId, Now, CancellationToken));

        issued.ExpiresAtUtc.Should().Be(Now.AddDays(30));
        await using var context = database.CreateContext();
        var stored = await context.Set<RefreshToken>().SingleAsync(token => token.UserId == userId, CancellationToken);
        stored.TokenHash.Should().HaveLength(RefreshToken.HashLength).And.NotContain(issued.Value);
    }

    [Fact]
    public async Task Redeem_ValidToken_ReturnsTheUserOnce()
    {
        var userId = await SaveUserAsync();
        var token = await IssueAsync(userId);

        (await RedeemAsync(token)).Should().Be(userId);
        (await RedeemAsync("unknown")).Should().BeNull();
    }

    [Fact]
    public async Task Redeem_UsedToken_RevokesEveryTokenOfTheUser()
    {
        var userId = await SaveUserAsync();
        var stolen = await IssueAsync(userId);
        (await RedeemAsync(stolen)).Should().Be(userId);
        var current = await IssueAsync(userId);

        await using (var context = database.CreateContext())
        {
            // The request fails, so it is not saved; the revocation must hold anyway.
            (await new RefreshTokenStore(context, _options).RedeemAsync(stolen, Now, CancellationToken)).Should().BeNull();
        }

        (await RedeemAsync(current)).Should().BeNull();
    }

    [Fact]
    public async Task Redeem_ExpiredOrRevokedToken_ReturnsNull()
    {
        var userId = await SaveUserAsync();
        var expired = await IssueAsync(userId);
        var revoked = await IssueAsync(userId);
        await RequestAsync(async store =>
        {
            await store.RevokeAsync(revoked, Now, CancellationToken);
            await store.RevokeAsync("unknown", Now, CancellationToken);

            return true;
        });

        (await RedeemAsync(expired, Now.AddDays(31))).Should().BeNull();
        (await RedeemAsync(revoked)).Should().BeNull();
    }

    [Fact]
    public async Task RevokeAll_EndsEverySessionOfTheUserOnly()
    {
        var userId = await SaveUserAsync();
        var otherUserId = await SaveUserAsync();
        var first = await IssueAsync(userId);
        var second = await IssueAsync(userId);
        var other = await IssueAsync(otherUserId);

        await RequestAsync(async store =>
        {
            await store.RevokeAllAsync(userId, Now, CancellationToken);

            return true;
        });

        (await RedeemAsync(first)).Should().BeNull();
        (await RedeemAsync(second)).Should().BeNull();
        (await RedeemAsync(other)).Should().Be(otherUserId);
    }

    [Fact]
    public async Task Redeem_SameTokenInTwoRequests_LetsOnlyOneSucceed()
    {
        var userId = await SaveUserAsync();
        var token = await IssueAsync(userId);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        (await new RefreshTokenStore(first, _options).RedeemAsync(token, Now, CancellationToken)).Should().Be(userId);
        (await new RefreshTokenStore(second, _options).RedeemAsync(token, Now, CancellationToken)).Should().Be(userId);
        await new UnitOfWork(first).SaveChangesAsync(CancellationToken);

        var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}

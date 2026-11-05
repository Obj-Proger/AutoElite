using AutoElite.Application.Users.Authentication;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoElite.Infrastructure.Tests.Authentication;

public sealed class AccessTokenTests
{
    // Only for these tests; a real key is a secret kept out of the repository.
    private const string SigningKey = "test-signing-key-that-is-long-enough-1234";

    private static readonly string[] _roleNames = [nameof(UserRole.Manager), nameof(UserRole.Student)];

    private static ServiceProvider Services(string signingKey = SigningKey) =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<ICurrentUser>(TestCurrentUser.System)
            .AddInfrastructure(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=autoelite",
                    ["Jwt:SigningKey"] = signingKey,
                    ["Jwt:AccessTokenLifetime"] = "00:10:00",
                    ["Client:BaseUrl"] = "https://school.example.ru/"
                })
                .Build())
            .BuildServiceProvider();

    private static User NewUser(Guid? branchId) =>
        User.Create(
                FullName.Create("Иванов", "Иван").Value, Email.Create("ivanov@example.com").Value, null, branchId,
                FixedClock.Default.UtcNow)
            .Value;

    private static Task<TokenValidationResult> ValidateAsync(ServiceProvider services, string token)
    {
        var bearer = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        return new JsonWebTokenHandler().ValidateTokenAsync(token, bearer.TokenValidationParameters);
    }

    [Fact]
    public async Task Create_TokenPassesTheHostsValidationAndCarriesTheUser()
    {
        await using var services = Services();
        var branchId = Guid.CreateVersion7();
        var user = NewUser(branchId);
        var utcNow = DateTime.UtcNow;

        var issued = services.GetRequiredService<IAccessTokenFactory>()
            .Create(user, [UserRole.Manager, UserRole.Student], utcNow);
        var validation = await ValidateAsync(services, issued.Value);

        validation.IsValid.Should().BeTrue(validation.Exception?.Message);
        issued.ExpiresAtUtc.Should().Be(utcNow.AddMinutes(10));
        var identity = validation.ClaimsIdentity;
        identity.FindFirst(AccessTokenClaims.UserId)!.Value.Should().Be(user.Id.ToString());
        identity.FindFirst(AccessTokenClaims.BranchId)!.Value.Should().Be(branchId.ToString());
        identity.FindAll(AccessTokenClaims.Role).Select(claim => claim.Value).Should().BeEquivalentTo(_roleNames);
    }

    [Fact]
    public async Task Create_UserWithoutBranch_LeavesTheBranchOut()
    {
        await using var services = Services();

        var issued = services.GetRequiredService<IAccessTokenFactory>().Create(NewUser(null), [], DateTime.UtcNow);

        (await ValidateAsync(services, issued.Value)).ClaimsIdentity.FindFirst(AccessTokenClaims.BranchId).Should().BeNull();
    }

    [Fact]
    public async Task Validate_TokenSignedWithAnotherKey_Fails()
    {
        await using var issuer = Services("another-signing-key-that-is-long-enough-5678");
        await using var host = Services();
        var issued = issuer.GetRequiredService<IAccessTokenFactory>().Create(NewUser(null), [], DateTime.UtcNow);

        (await ValidateAsync(host, issued.Value)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Settings_ShortSigningKey_AreRejected()
    {
        await using var services = Services("too-short");

        var read = () => services.GetRequiredService<IOptions<JwtOptions>>().Value;

        read.Should().Throw<OptionsValidationException>().WithMessage("*SigningKey*");
    }
}

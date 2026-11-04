using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeIdentityService : IIdentityService
{
    private readonly Dictionary<Guid, Account> _accounts = [];
    private int _tokenCount;

    public IReadOnlyDictionary<Guid, Account> Accounts => _accounts;

    public Task CreateAccountAsync(Guid userId, Email email, CancellationToken cancellationToken = default)
    {
        _accounts.Add(userId, new Account(email));

        return Task.CompletedTask;
    }

    public Task<bool> IsActivatedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.TryGetValue(userId, out var account) && account.Password is not null);

    public Task<string> GenerateInvitationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = $"invitation-{++_tokenCount}";
        _accounts[userId].InvitationTokens.Add(token);

        return Task.FromResult(token);
    }

    public Task<bool> AcceptInvitationAsync(Guid userId, string token, string password, CancellationToken cancellationToken = default)
    {
        var account = _accounts[userId];
        if (!account.InvitationTokens.Contains(token))
            return Task.FromResult(false);

        account.Password = password;
        account.EmailConfirmed = true;
        account.InvitationTokens.Clear();

        return Task.FromResult(true);
    }

    public Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var account = _accounts[userId];
        var result = account switch
        {
            { IsLockedOut: true } => PasswordCheckResult.LockedOut,
            _ when account.Password is null || account.Password != password => PasswordCheckResult.WrongPassword,
            { EmailConfirmed: false } => PasswordCheckResult.EmailNotConfirmed,
            _ => PasswordCheckResult.Succeeded
        };

        return Task.FromResult(result);
    }

    /// <summary>Adds an account that exists before the test acts.</summary>
    public void Seed(Guid userId, Email email) => _accounts.Add(userId, new Account(email));

    /// <summary>Marks the account as activated, as if the user had accepted the invitation.</summary>
    public void Activate(Guid userId, string password)
    {
        _accounts[userId].Password = password;
        _accounts[userId].EmailConfirmed = true;
    }

    internal sealed class Account(Email email)
    {
        public Email Email { get; } = email;

        public string? Password { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool IsLockedOut { get; set; }

        public List<string> InvitationTokens { get; } = [];
    }
}

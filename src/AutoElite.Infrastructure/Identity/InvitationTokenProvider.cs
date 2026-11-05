using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// Issues the token of an invitation link. It works like the default token provider but lives longer, because an
/// invited person may not read the email at once.
/// </summary>
internal sealed class InvitationTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<InvitationTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<UserAccount>> logger)
    : DataProtectorTokenProvider<UserAccount>(dataProtectionProvider, options, logger)
{
    /// <summary>The name the provider is registered under.</summary>
    public const string ProviderName = "Invitation";
}

/// <summary>The options of <see cref="InvitationTokenProvider"/>.</summary>
internal sealed class InvitationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    /// <summary>How long an invitation link is valid.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>Initializes the options with the name and lifetime of invitation tokens.</summary>
    public InvitationTokenProviderOptions()
    {
        Name = InvitationTokenProvider.ProviderName;
        TokenLifespan = Lifetime;
    }
}

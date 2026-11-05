using AutoElite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// Keeps accounts in the school's database. It only tracks changes, as repositories do: the unit of work saves them
/// with the rest of the request, so that an account and its user are created or changed in one transaction.
/// </summary>
/// <remarks>
/// The base store updates an account by attaching it again, which takes its current values as the values loaded from
/// the database. That is harmless when every update is saved at once, but here one request may update an account more
/// than once before the save, for example by setting the password and then confirming the address; the second attach
/// would then take the first update's concurrency stamp as the stored one, and the save would fail. An account this
/// context already tracks is therefore only given a new concurrency stamp, and the change tracker finds the changes.
/// </remarks>
internal sealed class AccountStore : UserOnlyStore<UserAccount, AutoEliteDbContext, Guid>
{
    /// <summary>Initializes the store on the context of the request.</summary>
    /// <param name="context">The context of the request.</param>
    /// <param name="describer">The describer of identity errors.</param>
    public AccountStore(AutoEliteDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    /// <inheritdoc />
    public override Task<IdentityResult> UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (Context.Entry(user).State == EntityState.Detached)
            return base.UpdateAsync(user, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        return Task.FromResult(IdentityResult.Success);
    }
}

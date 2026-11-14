namespace AutoElite.API.Authentication;

/// <summary>
/// The caller of a scope that the host creates to act on behalf of a user, such as the demo data of development: set
/// once, before anything of the scope reads its <see cref="ICurrentUser"/>. A request of the API never sets it, so
/// its caller is always the one of its access token (<see cref="HttpCurrentUser"/>).
/// </summary>
internal sealed class ScopeCaller
{
    /// <summary>Gets the user the scope acts on behalf of, or <see langword="null"/> for the caller of the request.</summary>
    public ICurrentUser? User { get; private set; }

    /// <summary>Makes the scope act on behalf of a user.</summary>
    /// <param name="user">The user.</param>
    /// <exception cref="InvalidOperationException">The scope already acts on behalf of a user.</exception>
    public void ActAs(ICurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (User is not null)
            throw new InvalidOperationException("The scope already acts on behalf of a user.");

        User = user;
    }
}

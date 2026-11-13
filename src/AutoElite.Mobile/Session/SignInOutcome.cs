namespace AutoElite.Mobile.Session;

/// <summary>How signing in ended.</summary>
public enum SignInOutcome
{
    /// <summary>The user is signed in.</summary>
    SignedIn,

    /// <summary>The email address or the password is wrong.</summary>
    WrongCredentials,

    /// <summary>The account is locked for a while after wrong passwords, not confirmed or deactivated.</summary>
    Blocked,

    /// <summary>The API limits the attempts to sign in; the user tries again later.</summary>
    TooManyAttempts,

    /// <summary>The API of the school could not be reached.</summary>
    Unreachable,

    /// <summary>The user is neither a student nor an instructor, so the application is not for them.</summary>
    NotForThisApp,

    /// <summary>The API failed otherwise.</summary>
    Failed
}

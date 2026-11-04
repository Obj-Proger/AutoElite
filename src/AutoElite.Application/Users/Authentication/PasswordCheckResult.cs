namespace AutoElite.Application.Users.Authentication;

/// <summary>The outcome of checking a user's password at sign-in.</summary>
public enum PasswordCheckResult
{
    /// <summary>The password is correct and the user may sign in.</summary>
    Succeeded = 1,

    /// <summary>The password is wrong, or the user has not set one yet.</summary>
    WrongPassword = 2,

    /// <summary>Too many wrong passwords in a row; sign-in is blocked for a while.</summary>
    LockedOut = 3,

    /// <summary>The password is correct, but the email address is not confirmed.</summary>
    EmailNotConfirmed = 4
}

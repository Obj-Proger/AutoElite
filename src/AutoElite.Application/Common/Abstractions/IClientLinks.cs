namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Builds links to pages of the web client: those sent by email, and its start page, where the mobile application
/// sends users for what it leaves to the website. The infrastructure knows the client's address and encodes the
/// parameters.
/// </summary>
public interface IClientLinks
{
    /// <summary>Gives the address of the start page of the web client, with a trailing slash.</summary>
    /// <returns>The address.</returns>
    Uri Website();

    /// <summary>Builds the link to the page where an invited user sets a password.</summary>
    /// <param name="userId">The identifier of the invited user.</param>
    /// <param name="token">The invitation token.</param>
    Uri Invitation(Guid userId, string token);

    /// <summary>Builds the link to the page where a user sets a new password after forgetting the old one.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="token">The password reset token.</param>
    Uri PasswordReset(Guid userId, string token);

    /// <summary>Builds the link that confirms a new email address of a user.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="newEmail">The new address, which the token is bound to.</param>
    /// <param name="token">The email change token.</param>
    Uri EmailChange(Guid userId, Email newEmail, string token);
}

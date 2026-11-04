namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Builds links to pages of the web client that are sent by email. The infrastructure knows the client's address
/// and encodes the parameters.
/// </summary>
public interface IClientLinks
{
    /// <summary>Builds the link to the page where an invited user sets a password.</summary>
    /// <param name="userId">The identifier of the invited user.</param>
    /// <param name="token">The invitation token.</param>
    Uri Invitation(Guid userId, string token);
}

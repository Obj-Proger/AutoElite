using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users;
using AutoElite.Application.Users.ChangeUserBranch;
using AutoElite.Application.Users.DeactivateUser;
using AutoElite.Application.Users.GetUser;
using AutoElite.Application.Users.GetUsers;
using AutoElite.Application.Users.GrantRole;
using AutoElite.Application.Users.InviteUser;
using AutoElite.Application.Users.ReactivateUser;
using AutoElite.Application.Users.ResendInvitation;
using AutoElite.Application.Users.RevokeRole;
using AutoElite.Application.Users.UpdateUserDetails;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The users of the school, for administrators and managers (FR-1.5, FR-1.6): inviting, reading, changing details,
/// roles and branch, deactivating. A manager works with the users of their own branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/users")]
public sealed class UsersController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Finds users, a page at a time.</summary>
    /// <param name="search">Text in the name, the email address or the phone.</param>
    /// <param name="role">A role the users have.</param>
    /// <param name="branchId">The branch of the users.</param>
    /// <param name="isActive">Whether the users are active.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of users on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of users.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<UserListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        string? search,
        UserRole? role,
        Guid? branchId,
        bool? isActive,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(new GetUsersQuery(search, role, branchId, isActive, page, pageSize), cancellationToken));

    /// <summary>Reads a user.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The user.</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<UserDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetUserQuery(userId), cancellationToken));

    /// <summary>Invites a user: registers them with their roles and emails a link to set a password.</summary>
    /// <param name="command">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the user.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> InviteUser(InviteUserCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetUser), "userId");

    /// <summary>Changes the name and the phone of a user.</summary>
    /// <param name="userId">The user; it takes the place of the one in the body.</param>
    /// <param name="command">The name and the phone.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateUserDetails(
        Guid userId, UpdateUserDetailsCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { UserId = userId }, cancellationToken));

    /// <summary>Moves a user to another branch, or takes them out of any (administrators only).</summary>
    /// <param name="userId">The user; it takes the place of the one in the body.</param>
    /// <param name="command">The branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{userId:guid}/branch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ChangeBranch(Guid userId, ChangeUserBranchCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { UserId = userId }, cancellationToken));

    /// <summary>Grants a role (administrators only).</summary>
    /// <param name="userId">The user.</param>
    /// <param name="role">The role.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{userId:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> GrantRole(Guid userId, UserRole role, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GrantRoleCommand(userId, role), cancellationToken));

    /// <summary>Revokes a role (administrators only).</summary>
    /// <param name="userId">The user.</param>
    /// <param name="role">The role.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{userId:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RevokeRole(Guid userId, UserRole role, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new RevokeRoleCommand(userId, role), cancellationToken));

    /// <summary>Deactivates a user: they can no longer sign in, and their sessions end.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateUserCommand(userId), cancellationToken));

    /// <summary>Reactivates a user.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{userId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid userId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateUserCommand(userId), cancellationToken));

    /// <summary>Sends the invitation again, with a new link, to a user who has not accepted it.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{userId:guid}/invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ResendInvitation(Guid userId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ResendInvitationCommand(userId), cancellationToken));
}

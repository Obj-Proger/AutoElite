using AutoElite.Application.Common.Messaging;
using AutoElite.Application.Users.InviteFirstAdministrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// Invites the first administrator of <see cref="FirstAdministratorOptions"/> when the host starts, after the
/// migrations. A school that has an active administrator is left as it is, so the settings may stay in place. A
/// failure, such as an address already taken, is logged and the host starts anyway, so that the rest of the school
/// can work while the settings are corrected.
/// </summary>
internal sealed partial class FirstAdministratorInviter(
    IServiceScopeFactory scopeFactory,
    IOptions<FirstAdministratorOptions> options,
    ILogger<FirstAdministratorInviter> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var administrator = options.Value;
        if (string.IsNullOrWhiteSpace(administrator.Email))
            return;

        // Outside a request the caller is the system, the only one that may send the command.
        await using var scope = scopeFactory.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(
            new InviteFirstAdministratorCommand(
                administrator.LastName, administrator.FirstName, administrator.Email, administrator.PreferredLanguage),
            cancellationToken);

        if (result.IsFailure)
            LogInvitationFailed(logger, result.Error.Code, result.Error.Message);
        else if (result.Value is { } userId)
            LogInvited(logger, userId);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Invited the first administrator {UserId}")]
    private static partial void LogInvited(ILogger logger, Guid userId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The first administrator of the settings could not be invited: {ErrorCode} {ErrorMessage}")]
    private static partial void LogInvitationFailed(ILogger logger, string errorCode, string errorMessage);
}

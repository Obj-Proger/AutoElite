using AutoElite.API.Authentication;
using AutoElite.Application.Users;
using AutoElite.Application.Users.AcceptInvitation;
using AutoElite.Domain.Entities;
using AutoElite.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace AutoElite.API.DemoData;

/// <summary>
/// Creates the demo data when the host starts, once, in a school that does not have it yet: a school three weeks into
/// the training of a group, with every part of the system in use. Each step is a command of the application, sent on
/// behalf of the demo user who would take it, such as the theory instructor who schedules their lessons, so that the
/// data passes the same rules and has the same side effects as data entered by hand; the past, such as the lessons
/// already held, is created with the <see cref="DemoClock"/> turned back to when it happened. Only the users are
/// registered directly, as an invitation would, with the password of the options. A failure is logged and the host
/// starts anyway; a new database starts again.
/// </summary>
internal sealed partial class DemoDataSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoDataOptions> options,
    DemoClock clock,
    ILogger<DemoDataSeeder> logger) : IHostedService
{
    /// <summary>The domain of the addresses of the demo users, reserved for examples, so that no email reaches anybody.</summary>
    public const string EmailDomain = "autoelite.example";

    private const string TimeZoneId = "Asia/Yekaterinburg";

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Password is not { Length: > 0 } password)
        {
            LogNoPassword(logger);

            return;
        }

        try
        {
            if (await HasDemoDataAsync(cancellationToken))
                return;

            LogCreating(logger);
            await CreateAsync(password, cancellationToken);
            LogCreated(logger, EmailDomain);
        }
        catch (InvalidOperationException exception)
        {
            LogFailed(logger, exception.Message);
        }
        finally
        {
            clock.Reset();
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Email EmailOf(string name) => Email.Create($"{name}@{EmailDomain}").Value;

    // The local time of the branch on a day, in UTC.
    private static DateTime UtcOf(DateOnly day, int hour, int minute, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(new TimeOnly(hour, minute)), zone);

    private async Task<bool> HasDemoDataAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        return await users.EmailExistsAsync(EmailOf("admin"), cancellationToken);
    }

    // Registers a user with a role and a sign-in account, as an invitation does, and accepts the invitation with the
    // password, as the user would.
    private async Task<DemoCaller> RegisterAsync(
        string name,
        string lastName,
        string firstName,
        string? middleName,
        string? phone,
        UserRole role,
        Guid? branchId,
        string password,
        CancellationToken cancellationToken)
    {
        var user = User.Create(
                FullName.Create(lastName, firstName, middleName).Value,
                EmailOf(name),
                phone is null ? null : PhoneNumber.Create(phone).Value,
                branchId,
                clock.GetUtcNow().UtcDateTime,
                Language.Russian)
            .Value;

        string token;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            services.GetRequiredService<IUserRepository>().Add(user);
            services.GetRequiredService<IUserRoleRepository>().Add(UserRoleAssignment.Create(user.Id, role).Value);
            var identity = services.GetRequiredService<IIdentityService>();
            await identity.CreateAccountAsync(user.Id, user.Email, cancellationToken);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            token = await identity.GenerateInvitationTokenAsync(user.Id, cancellationToken);
        }

        await SendAsync(null, new AcceptInvitationCommand(user.Id, token, password), cancellationToken);

        return new DemoCaller(user.Id, branchId, role);
    }

    private async Task<TValue> SendAsync<TValue>(DemoCaller caller, ICommand<TValue> command, CancellationToken cancellationToken) =>
        (await SendAsync<Result<TValue>>(caller, command, cancellationToken)).Value;

    private async Task<TValue> QueryAsync<TValue>(DemoCaller caller, IQuery<TValue> query, CancellationToken cancellationToken) =>
        (await SendAsync<Result<TValue>>(caller, query, cancellationToken)).Value;

    // Sends a command on behalf of a demo user, or of nobody, in a scope of its own, as a request of the API would be.
    private async Task<TResult> SendAsync<TResult>(DemoCaller? caller, IRequest<TResult> command, CancellationToken cancellationToken)
        where TResult : Result
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        if (caller is not null)
            scope.ServiceProvider.GetRequiredService<ScopeCaller>().ActAs(caller);

        var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(command, cancellationToken);

        return result.IsFailure
            ? throw new InvalidOperationException($"{command.GetType().Name} failed: {result.Error.Code} {result.Error.Message}")
            : result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "DemoData:Password is not set, so no demo data was created")]
    private static partial void LogNoPassword(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating the demo data, which takes a while; the API answers once it is created")]
    private static partial void LogCreating(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the demo data; its users sign in at @{EmailDomain}")]
    private static partial void LogCreated(ILogger logger, string emailDomain);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The demo data could not be created: {Reason}. Start with a new database to try again")]
    private static partial void LogFailed(ILogger logger, string reason);
}

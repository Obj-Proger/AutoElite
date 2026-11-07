using AutoElite.Application.Common.Messaging;
using AutoElite.Application.Notifications.SendReminders;
using AutoElite.Application.StaffTasks.RemindOverdueStaffTasks;
using Hangfire;

namespace AutoElite.Infrastructure.BackgroundJobs;

/// <summary>
/// The scheduled jobs that send system commands of the application (D-058). Hangfire runs each in a scope of its own,
/// which is no request, so the host's <see cref="ICurrentUser"/> reports the system there.
/// </summary>
/// <remarks>
/// The commands must not overlap, so a run waits for the one before; a failed run is not retried, since the next one
/// comes within minutes and does the same work. A failure is thrown, so that Hangfire shows it among the failed jobs.
/// </remarks>
/// <param name="dispatcher">The dispatcher of the application.</param>
internal sealed class SystemJobs(IDispatcher dispatcher)
{
    /// <summary>How long a run waits for the one before it to finish, in seconds.</summary>
    private const int OverlapTimeoutSeconds = 10 * 60;

    /// <summary>Reminds the participants of upcoming lessons and exams.</summary>
    /// <param name="cancellationToken">The token Hangfire cancels when the server stops.</param>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(OverlapTimeoutSeconds)]
    public Task SendRemindersAsync(CancellationToken cancellationToken) =>
        SendAsync(new SendRemindersCommand(), cancellationToken);

    /// <summary>Reminds the assignees of overdue staff tasks.</summary>
    /// <param name="cancellationToken">The token Hangfire cancels when the server stops.</param>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(OverlapTimeoutSeconds)]
    public Task RemindOverdueStaffTasksAsync(CancellationToken cancellationToken) =>
        SendAsync(new RemindOverdueStaffTasksCommand(), cancellationToken);

    private async Task SendAsync(ICommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"{command.GetType().Name} failed: [{result.Error.Code}] {result.Error.Message}");
        }
    }
}

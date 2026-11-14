using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using AutoElite.Client.Authentication;
using Microsoft.JSInterop;

namespace AutoElite.Web.Authentication;

/// <summary>
/// Lets one tab of the browser at a time renew the session of the website, with a lock of the Web Locks API that every
/// tab of the site shares (wwwroot/js/session-lock.js).
/// </summary>
/// <param name="js">The JavaScript of the page.</param>
internal sealed class BrowserTabsLock(IJSRuntime js) : ISessionRenewalLock
{
    private const string LockName = "autoelite.session-renewal";

    private Task<IJSObjectReference>? _module;

    /// <inheritdoc />
    public async Task RunAsync(Func<Task> renewal, CancellationToken cancellationToken)
    {
        _module ??= js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/session-lock.js").AsTask();
        var module = await _module;
        var exclusive = new ExclusiveRenewal(renewal);
        using var reference = DotNetObjectReference.Create(exclusive);
        await module.InvokeVoidAsync("runExclusively", cancellationToken, LockName, reference);
        exclusive.Failure?.Throw();
    }

    /// <summary>The renewal that the lock runs, which keeps its failure for the caller.</summary>
    /// <param name="renewal">The renewal.</param>
    internal sealed class ExclusiveRenewal(Func<Task> renewal)
    {
        /// <summary>Gets the failure of the renewal, if it failed.</summary>
        public ExceptionDispatchInfo? Failure { get; private set; }

        /// <summary>Runs the renewal, called by the lock.</summary>
        /// <returns>A task that completes when the renewal has run.</returns>
        [JSInvokable]
        [SuppressMessage("Design", "CA1031", Justification = "The failure is given to the caller as it is, whatever it is.")]
        public async Task Run()
        {
            try
            {
                await renewal();
            }
            catch (Exception exception)
            {
                Failure = ExceptionDispatchInfo.Capture(exception);
            }
        }
    }
}

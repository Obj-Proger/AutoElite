using System.Text.Json;
using AutoElite.API.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoElite.API.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class GlobalExceptionHandlerTests(ApiFactory api) : IDisposable
{
    private readonly IServiceScope _scope = api.Services.CreateScope();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FailureOfTheServer_Is500_WithoutItsCause()
    {
        var (handled, context) = await HandleAsync(new InvalidOperationException("Connection string: Password=secret"));

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
        var body = await BodyAsync(context);
        body.Should().NotContain("secret").And.NotContain(nameof(InvalidOperationException));
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task RequestTheServerCouldNotRead_KeepsItsStatus()
    {
        var tooLarge = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);

        var (_, context) = await HandleAsync(tooLarge);

        context.Response.StatusCode.Should().Be(413);
    }

    [Fact]
    public async Task RequestTheClientAbandoned_GetsNoAnswer()
    {
        var (handled, context) = await HandleAsync(new OperationCanceledException(), abandoned: true);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
        context.Response.Body.Length.Should().Be(0);
    }

    private async Task<(bool Handled, DefaultHttpContext Context)> HandleAsync(Exception exception, bool abandoned = false)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            RequestAborted = new CancellationToken(canceled: abandoned)
        };
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            _scope.ServiceProvider.GetRequiredService<IProblemDetailsService>(), NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken);

        return (handled, context);
    }

    private static async Task<string> BodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);

        return await reader.ReadToEndAsync(CancellationToken);
    }
}

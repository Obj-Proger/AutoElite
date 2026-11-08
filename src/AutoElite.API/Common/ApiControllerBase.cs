using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Common;

/// <summary>
/// The base of the controllers of the public API (D-002): each action sends one command or query through the dispatcher
/// and turns its result into a response, the value on success and problem details (<see cref="ApiProblems"/>) on
/// failure. Access is checked by the application, so the controllers carry no authorization attributes.
/// </summary>
/// <remarks>
/// JSON is the only format the controllers write, so no <see cref="ProducesAttribute"/> narrows it: that attribute
/// would also turn the <c>application/problem+json</c> of the errors into <c>application/json</c>.
/// </remarks>
[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Responds to a result without a value: 204 on success.</summary>
    /// <param name="result">The result.</param>
    protected IActionResult Respond(Result result) => result.IsSuccess ? NoContent() : Failure(result.Error);

    /// <summary>Responds to a result with a value: 200 with the value on success.</summary>
    /// <param name="result">The result.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    protected IActionResult Respond<T>(Result<T> result) => result.IsSuccess ? Ok(result.Value) : Failure(result.Error);

    /// <summary>Responds with the problem details of an error.</summary>
    /// <param name="error">The error.</param>
    protected ObjectResult Failure(Error error)
    {
        var problem = ApiProblems.Create(HttpContext, error);

        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
    }
}

using System.Globalization;
using AutoElite.API.Common;
using AutoElite.Application.Common;
using AutoElite.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.API.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class ApiProblemsTests(ApiFactory api) : IDisposable
{
    private static readonly string[] _unreadableFields = ["items[0].price", "branchId"];

    private readonly IServiceScope _scope = api.Services.CreateScope();

    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    private DefaultHttpContext Request() => new() { RequestServices = _scope.ServiceProvider };

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _uiCulture;
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("FirstName", "firstName")]
    [InlineData("Items[0].Price", "items[0].price")]
    [InlineData("Contract.StudentId", "contract.studentId")]
    [InlineData("rows[12]", "rows[12]")]
    [InlineData("file[Questions/Ticket 1.JSON]", "file[Questions/Ticket 1.JSON]")]
    [InlineData("", "")]
    public void JsonFieldName_WritesFieldsAsTheJsonOfTheRequest(string field, string expected)
    {
        ApiProblems.JsonFieldName(field).Should().Be(expected);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 500)]
    public void StatusCode_FollowsTheTypeOfTheError(ErrorType type, int status)
    {
        ApiProblems.StatusCodeOf(type).Should().Be(status);
    }

    [Fact]
    public void Create_ValidationError_IsTranslatedWithItsCodeAndCamelCaseFields()
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru");
        var unreadable = ApplicationErrors.Request.UnreadableValue;
        var error = ApplicationErrors.ValidationFailed([new FieldError("Items[0].Price", unreadable.Code, unreadable.Message)]);

        var problem = ApiProblems.Create(Request(), error);

        problem.Status.Should().Be(400);
        problem.Detail.Should().Be("Одна или несколько ошибок проверки данных.");
        problem.Extensions[ApiProblems.CodeExtension].Should().Be(ApplicationErrors.ValidationFailedCode);
        problem.Extensions[ApiProblems.ErrorsExtension].Should().BeEquivalentTo(
            new[] { new ApiFieldError("items[0].price", unreadable.Code, "Значение не удалось прочитать.") });
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public void Create_ErrorWithoutFields_HasNoFieldList()
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

        var problem = ApiProblems.Create(Request(), ApplicationErrors.Auth.Forbidden);

        problem.Status.Should().Be(403);
        problem.Detail.Should().Be(ApplicationErrors.Auth.Forbidden.Message);
        problem.Extensions.Should().NotContainKey(ApiProblems.ErrorsExtension);
    }

    [Fact]
    public void FromModelState_ListsTheUnreadableFieldsOfTheBody()
    {
        var context = new ActionContext(Request(), new RouteData(), new ActionDescriptor());
        context.ModelState.AddModelError("$.items[0].price", "The JSON value could not be converted.");
        context.ModelState.AddModelError("$", "The JSON is malformed.");
        context.ModelState.AddModelError("BranchId", "The value 'x' is not valid.");

        var result = ApiProblems.FromModelState(context);

        var problem = result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Extensions[ApiProblems.CodeExtension].Should().Be(ApplicationErrors.ValidationFailedCode);
        problem.Extensions[ApiProblems.ErrorsExtension].As<IEnumerable<ApiFieldError>>()
            .Select(fieldError => fieldError.Field)
            .Should().BeEquivalentTo(_unreadableFields);
    }
}

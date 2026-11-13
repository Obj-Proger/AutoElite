using System.Globalization;
using System.Text.RegularExpressions;
using AutoElite.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.API.Tests;

/// <summary>
/// Calls every GET endpoint of the OpenAPI document, with the values its required parameters need, on the data the
/// other tests leave: whatever the answer, it is never a server error. It catches a query that the database cannot
/// run or a read model that cannot be made, in an endpoint no other test calls with such data.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed partial class GetEndpointsSmokeTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static TheoryData<UserRole> Roles => [UserRole.Administrator, UserRole.Student];

    [Theory]
    [MemberData(nameof(Roles))]
    public async Task GetEndpoints_NeverAnswerWithAServerError(UserRole role)
    {
        using var client = await api.SignedInClientAsync(role);
        var endpoints = api.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description => description.HttpMethod == HttpMethods.Get)
            .ToList();
        var failures = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var uri = UriOf(endpoint);
            using var response = await client.GetAsync(uri, CancellationToken);
            if ((int)response.StatusCode >= StatusCodes.Status500InternalServerError)
                failures.Add($"GET {uri}: {(int)response.StatusCode}");
        }

        endpoints.Should().HaveCountGreaterThan(50);
        failures.Should().BeEmpty();
    }

    private static string UriOf(ApiDescription endpoint)
    {
        var parameters = endpoint.ParameterDescriptions;
        var path = RouteParameter().Replace(
            endpoint.RelativePath!,
            match => ValueOf(parameters.First(parameter => parameter.Name == match.Groups["name"].Value)));
        var query = parameters
            .Where(parameter => parameter.Source == BindingSource.Query && IsRequired(parameter))
            .Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(ValueOf(parameter))}");

        return $"/{path}?{string.Join('&', query)}";
    }

    // A value type without a default value, which the endpoint cannot do without.
    private static bool IsRequired(ApiParameterDescription parameter) =>
        parameter.ParameterDescriptor is ControllerParameterDescriptor { ParameterInfo: var info }
        && !info.HasDefaultValue
        && info.ParameterType.IsValueType
        && Nullable.GetUnderlyingType(info.ParameterType) is null;

    // A period that starts yesterday and ends in a week; the first value of an enumeration; any identifier.
    private static string ValueOf(ApiParameterDescription parameter)
    {
        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;
        var endOfPeriod = parameter.Name.StartsWith("to", StringComparison.OrdinalIgnoreCase);
        var day = DateTime.UtcNow.Date.AddDays(endOfPeriod ? 7 : -1);

        return type switch
        {
            _ when type == typeof(Guid) => Guid.NewGuid().ToString(),
            _ when type == typeof(DateTime) => day.ToString("O", CultureInfo.InvariantCulture),
            _ when type == typeof(DateOnly) => DateOnly.FromDateTime(day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ when type.IsEnum => Enum.GetNames(type)[0],
            _ when type == typeof(bool) => "false",
            _ => "1"
        };
    }

    [GeneratedRegex(@"\{(?<name>[^}:?]+)[^}]*\}")]
    private static partial Regex RouteParameter();
}

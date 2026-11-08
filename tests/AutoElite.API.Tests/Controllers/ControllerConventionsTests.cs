using System.Reflection;
using AutoElite.API.Common;
using AutoElite.Application.Common.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Tests.Controllers;

public sealed class ControllerConventionsTests
{
    private static readonly Type[] _controllers =
    [
        .. typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
    ];

    [Fact]
    public void Controllers_Exist()
    {
        _controllers.Should().NotBeEmpty();
    }

    [Fact]
    public void Controllers_AreSealedApiControllers()
    {
        _controllers.Should().OnlyContain(type => type.IsSealed && type.IsSubclassOf(typeof(ApiControllerBase)));
    }

    [Fact]
    public void Controllers_AreUnderTheVersionPrefix()
    {
        _controllers.Should().OnlyContain(type =>
            type.GetCustomAttribute<RouteAttribute>() != null
            && type.GetCustomAttribute<RouteAttribute>()!.Template.StartsWith(ApiRoutes.Prefix + "/", StringComparison.Ordinal));
    }

    [Fact]
    public void Controllers_DependOnlyOnTheDispatcher()
    {
        _controllers.SelectMany(type => type.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters())
            .Should().OnlyContain(parameter => parameter.ParameterType == typeof(IDispatcher));
    }
}

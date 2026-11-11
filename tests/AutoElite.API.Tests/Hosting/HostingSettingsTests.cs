using System.Net;
using AutoElite.API.Hosting;
using Microsoft.AspNetCore.Builder;

namespace AutoElite.API.Tests.Hosting;

public sealed class HostingSettingsTests
{
    [Fact]
    public void ReverseProxy_WithAddressesAndNetworks_TrustsThemBesidesTheLocalProxy()
    {
        var settings = new ReverseProxySettings { KnownProxies = { "10.0.0.5" }, KnownNetworks = { "172.18.0.0/16" } };
        var options = new ForwardedHeadersOptions();
        var localProxies = options.KnownProxies.Count;

        settings.ApplyTo(options);

        settings.Problems().Should().BeEmpty();
        options.KnownProxies.Should().HaveCount(localProxies + 1).And.Contain(IPAddress.Parse("10.0.0.5"));
        options.KnownIPNetworks.Should().Contain(System.Net.IPNetwork.Parse("172.18.0.0/16"));
    }

    [Fact]
    public void ReverseProxy_WithWrongEntries_TellsEach()
    {
        var settings = new ReverseProxySettings { KnownProxies = { "proxy.local" }, KnownNetworks = { "10.0.0.0/33" } };

        settings.Problems().Should().HaveCount(2)
            .And.Contain(problem => problem.Contains("proxy.local", StringComparison.Ordinal))
            .And.Contain(problem => problem.Contains("10.0.0.0/33", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://admin.example.ru", "https://admin.example.ru")]
    [InlineData("https://admin.example.ru/", "https://admin.example.ru")]
    [InlineData("http://localhost:5174", "http://localhost:5174")]
    [InlineData("https://admin.example.ru/app", null)]
    [InlineData("ftp://admin.example.ru", null)]
    [InlineData("admin.example.ru", null)]
    public void CorsOrigin_IsASchemeAHostAndAPortOnly(string text, string? origin)
    {
        CorsSettings.OriginOf(text).Should().Be(origin);
    }

    [Fact]
    public void RateLimits_MustBePositive()
    {
        var settings = new RateLimitingSettings { PermitsPerMinute = 0, SensitivePermitsPerMinute = -1 };

        settings.Problems().Should().HaveCount(2);
    }
}

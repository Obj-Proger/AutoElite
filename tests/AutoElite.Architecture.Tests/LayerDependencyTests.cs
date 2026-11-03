namespace AutoElite.Architecture.Tests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_ReferencesOnlyTheBaseClassLibrary()
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Domain);

        references.Should().OnlyContain(
            name => name == "System" || name.StartsWith("System.", StringComparison.Ordinal),
            "the domain must not depend on other layers or on frameworks");
    }

    [Theory]
    [InlineData("AutoElite.Infrastructure")]
    [InlineData("AutoElite.API")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    public void Application_DoesNotReferenceOuterLayersOrDataAccess(string forbiddenPrefix)
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Application);

        references.Should().NotContain(name => name.StartsWith(forbiddenPrefix, StringComparison.Ordinal));
    }

    [Fact]
    public void Application_ReferencesTheDomain()
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Application);

        references.Should().Contain("AutoElite.Domain");
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceTheApi()
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Infrastructure);

        references.Should().NotContain(name => name.StartsWith("AutoElite.API", StringComparison.Ordinal));
    }
}

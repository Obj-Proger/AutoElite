namespace AutoElite.Architecture.Tests;

/// <summary>The production assemblies whose structure the tests check.</summary>
internal static class Layers
{
    public static readonly Assembly Domain = typeof(BaseEntity).Assembly;

    public static readonly Assembly Application = typeof(IDispatcher).Assembly;

    // The project has no types yet, so it is loaded by name; the project reference copies it to the output.
    public static readonly Assembly Infrastructure = Assembly.Load("AutoElite.Infrastructure");

    /// <summary>Gets the simple names of the assemblies that <paramref name="assembly"/> references.</summary>
    public static IReadOnlyList<string> ReferencedAssemblyNames(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty)];

    /// <summary>Gets the non-abstract classes of <paramref name="assembly"/> that implement the open generic interface.</summary>
    public static IReadOnlyList<Type> ClassesImplementing(Assembly assembly, Type openGenericInterface) =>
        [.. assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false }
            && type.GetInterfaces().Any(implemented =>
                implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openGenericInterface))];

    /// <summary>Gets the non-abstract classes of <paramref name="assembly"/> derived from <paramref name="baseType"/>.</summary>
    public static IReadOnlyList<Type> ClassesDerivedFrom(Assembly assembly, Type baseType) =>
        [.. assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(baseType))];
}

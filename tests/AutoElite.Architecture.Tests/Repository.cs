namespace AutoElite.Architecture.Tests;

/// <summary>The files of the repository that the tests read, such as those of projects the tests do not reference.</summary>
internal static class Repository
{
    /// <summary>Gets the root folder of the repository, the one with the solution file.</summary>
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AutoElite.slnx")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
        }
    }

    /// <summary>Gets the full path of a file or folder of the repository.</summary>
    /// <param name="parts">The parts of the path from the root.</param>
    /// <returns>The full path.</returns>
    public static string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);
}

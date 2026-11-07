using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>
/// The database objects EF Core does not model, such as exclusion constraints against overlapping times. They are SQL
/// scripts in <c>Persistence/Sql</c>, embedded in this assembly, which the migrations apply with
/// <see cref="Create"/> and <see cref="Drop"/>.
/// </summary>
/// <remarks>
/// A migration names the scripts it applies, so that it keeps doing the same thing when later migrations add scripts.
/// A script, once a migration applies it, does not change: a change to the objects is a new script and a new
/// migration that drops the old objects and creates the new ones.
/// </remarks>
internal static class DatabaseObjects
{
    /// <summary>Creates the objects of the scripts, in the given order, as part of a migration.</summary>
    /// <param name="migrationBuilder">The builder of the migration.</param>
    /// <param name="scripts">The names of the scripts, without the <c>.up.sql</c> ending.</param>
    public static void Create(MigrationBuilder migrationBuilder, params string[] scripts)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        foreach (var script in scripts)
            migrationBuilder.Sql(Read($"{script}.up.sql"));
    }

    /// <summary>Drops the objects of the scripts, in the reverse order, as part of reverting a migration.</summary>
    /// <param name="migrationBuilder">The builder of the migration.</param>
    /// <param name="scripts">The names of the scripts as <see cref="Create"/> was given them.</param>
    public static void Drop(MigrationBuilder migrationBuilder, params string[] scripts)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        foreach (var script in Enumerable.Reverse(scripts))
            migrationBuilder.Sql(Read($"{script}.down.sql"));
    }

    private static string Read(string fileName)
    {
        var resourceName = $"Sql/{fileName}";
        using var stream = typeof(DatabaseObjects).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded SQL script {resourceName} is missing.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
